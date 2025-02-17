using Crpg.Module.Api;
using Crpg.Module.Api.Models;
using Crpg.Module.Api.Models.Characters;
using Crpg.Module.Common;
using Crpg.Module.Common.Network;
using Crpg.Module.Modes.Warmup;
using Crpg.Module.Rating;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;
using MathF = TaleWorlds.Library.MathF;

namespace Crpg.Module.Rewards;

/// <summary>
/// Gives xp/gold, update rating and stats.
/// </summary>
internal class CrpgRewardServer : MissionBehavior
{
    /// <summary>When there is no round controller (e.g. siege), rewards are sent every minute.</summary>
    private const int TickDuration = 60;

    private readonly ICrpgClient _crpgClient;
    private readonly CrpgConstants _constants;
    private readonly CrpgWarmupComponent _warmupComponent;
    private readonly MultiplayerRoundController? _roundController;
    private readonly Dictionary<PlayerId, CrpgPlayerRating> _characterRatings;
    private readonly CrpgRatingPeriodResults _ratingResults;
    private readonly Random _random;
    private readonly PeriodStatsHelper _periodStatsHelper;

    private MissionTimer? _tickTimer;
    private bool _lastRewardDuringHappyHours;

    public CrpgRewardServer(
        ICrpgClient crpgClient,
        CrpgConstants constants,
        CrpgWarmupComponent warmupComponent,
        MultiplayerRoundController? roundController)
    {
        _crpgClient = crpgClient;
        _constants = constants;
        _warmupComponent = warmupComponent;
        _roundController = roundController;
        _characterRatings = new Dictionary<PlayerId, CrpgPlayerRating>();
        _ratingResults = new CrpgRatingPeriodResults();
        _random = new Random();
        _periodStatsHelper = new PeriodStatsHelper();
        _lastRewardDuringHappyHours = false;
    }

    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

    public override void AfterStart()
    {
        base.AfterStart();
        if (_roundController != null)
        {
            _roundController.OnPreRoundEnding += OnPreRoundEnding;
        }
        else
        {
            _warmupComponent.OnWarmupEnded += OnWarmupEnded;
        }
    }

    public override void OnRemoveBehavior()
    {
        if (_roundController != null)
        {
            _roundController.OnPreRoundEnding -= OnPreRoundEnding;
        }
        else
        {
            _warmupComponent.OnWarmupEnded -= OnWarmupEnded;
        }

        base.OnRemoveBehavior();
    }

    public override void OnMissionTick(float dt)
    {
        if (_tickTimer != null && _tickTimer.Check(reset: true))
        {
            _ = UpdateCrpgUsersAsync(_tickTimer.GetTimerDuration());
        }
    }

    public override void OnAgentHit(
        Agent affectedAgent,
        Agent affectorAgent,
        in MissionWeapon affectorWeapon,
        in Blow blow,
        in AttackCollisionData attackCollisionData)
    {
        if (_warmupComponent.IsInWarmup)
        {
            return;
        }

        if (affectedAgent == affectorAgent) // Self hit.
        {
            return;
        }

        if (affectedAgent.Team == affectorAgent.Team) // Team hit.
        {
            return; // TODO: should penalize affector.
        }

        if (!TryGetRating(affectedAgent, out var affectedRating)
            || !TryGetRating(affectorAgent, out var affectorRating))
        {
            return;
        }

        float inflictedRatio = MathF.Clamp(blow.InflictedDamage / affectedAgent.BaseHealthLimit, 0f, 1f);
        _ratingResults.AddResult(affectorRating!, affectedRating!, inflictedRatio);
    }

    private bool TryGetRating(Agent agent, out CrpgPlayerRating? rating)
    {
        rating = null!;
        if (agent.MissionPeer == null)
        {
            return false;
        }

        if (!_characterRatings.TryGetValue(agent.MissionPeer.Peer.Id, out rating))
        {
            var crpgPeer = agent.MissionPeer.Peer.GetComponent<CrpgPeer>();
            if (crpgPeer?.User == null)
            {
                return false;
            }

            // If the user has no region yet or they are not playing locally, act like they weren't there. That is, don't
            // change their rating or their opponent rating.
            if (crpgPeer.User.Region != CrpgServerConfiguration.Region)
            {
                return false;
            }

            var characterRating = crpgPeer.User.Character.Rating;
            rating = new CrpgPlayerRating(characterRating.Value, characterRating.Deviation, characterRating.Volatility);
            _characterRatings[agent.MissionPeer.Peer.Id] = rating;
            _ratingResults.AddParticipant(rating);
        }

        return true;
    }

    private void OnPreRoundEnding()
    {
        float duration = MultiplayerOptions.OptionType.RoundTimeLimit.GetIntValue() - _roundController!.RemainingRoundTime;
        _ = UpdateCrpgUsersAsync(duration);
    }

    private void OnWarmupEnded()
    {
        _tickTimer = new MissionTimer(TickDuration);
    }

    private async Task UpdateCrpgUsersAsync(float durationRewarded)
    {
        CrpgRatingCalculator.UpdateRatings(_ratingResults);

        Dictionary<int, CrpgPeer> crpgPeerByUserId = new();
        Dictionary<PlayerId, PeriodStats> periodStats = _periodStatsHelper.ComputePeriodStats();
        List<CrpgUserUpdate> userUpdates = new();
        var networkPeers = GameNetwork.NetworkPeers.ToArray();
        bool lowPopulationServer = networkPeers.Length <= 4;
        var valorousPlayerIds = GetValorousPlayers(networkPeers, periodStats, !lowPopulationServer);
        foreach (NetworkCommunicator networkPeer in networkPeers)
        {
            var playerId = networkPeer.VirtualPlayer.Id;

            var missionPeer = networkPeer.GetComponent<MissionPeer>();
            var crpgPeer = networkPeer.GetComponent<CrpgPeer>();
            if (missionPeer == null || crpgPeer?.User == null)
            {
                continue;
            }

            crpgPeerByUserId[crpgPeer.User.Id] = crpgPeer;

            CrpgUserUpdate userUpdate = new()
            {
                CharacterId = crpgPeer.User.Character.Id,
                Reward = new CrpgUserReward { Experience = 0, Gold = 0 },
                Statistics = new CrpgCharacterStatistics { Kills = 0, Deaths = 0, Assists = 0, PlayTime = TimeSpan.Zero },
                Rating = crpgPeer.User.Character.Rating,
                BrokenItems = Array.Empty<CrpgUserDamagedItem>(),
            };

            if (CrpgFeatureFlags.IsEnabled(CrpgFeatureFlags.FeatureTournament))
            {
                userUpdates.Add(userUpdate);
                continue;
            }

            userUpdate.Rating = GetNewRating(crpgPeer);

            if ((_roundController != null && crpgPeer.SpawnTeamThisRound != null)
                || (_roundController == null && missionPeer.Team != null && missionPeer.Team.Side != BattleSideEnum.None))
            {
                bool isValorousPlayer = valorousPlayerIds.Contains(playerId);
                SetReward(userUpdate, crpgPeer, durationRewarded, isValorousPlayer, !lowPopulationServer);
                SetStatistics(userUpdate, networkPeer, periodStats);
                userUpdate.BrokenItems = BreakItems(crpgPeer, durationRewarded * (lowPopulationServer ? 0.5f : 1f));
            }

            userUpdates.Add(userUpdate);
        }

        _ratingResults.Clear();
        _characterRatings.Clear();

        if (userUpdates.Count == 0)
        {
            return;
        }

        // TODO: add retry mechanism (the endpoint need to be idempotent though).
        try
        {
            var res = (await _crpgClient.UpdateUsersAsync(new CrpgGameUsersUpdateRequest { Updates = userUpdates })).Data!;
            SendRewardToPeers(res.UpdateResults, crpgPeerByUserId, valorousPlayerIds);
        }
        catch (Exception e)
        {
            Debug.Print("Couldn't update users: " + e);
            SendErrorToPeers(crpgPeerByUserId);
        }
    }

    private void SetReward(CrpgUserUpdate userUpdate, CrpgPeer crpgPeer, float durationRewarded, bool isValorousPlayer,
        bool rewardMultiplierEnabled)
    {
        float serverXpMultiplier = CrpgServerConfiguration.ServerExperienceMultiplier;
        serverXpMultiplier *= IsHappyHour() ? 1.5f : 1f;
        userUpdate.Reward = new CrpgUserReward
        {
            Experience = (int)(serverXpMultiplier * durationRewarded * (_constants.BaseExperienceGainPerSecond
                + crpgPeer.RewardMultiplier * _constants.MultipliedExperienceGainPerSecond)),
            Gold = (int)(durationRewarded * (_constants.BaseGoldGainPerSecond
                + crpgPeer.RewardMultiplier * _constants.MultipliedGoldGainPerSecond)),
        };

        if (!rewardMultiplierEnabled)
        {
            crpgPeer.RewardMultiplier = 1;
        }
        else if (_roundController == null)
        {
            crpgPeer.RewardMultiplier = 2;
        }
        else
        {
            int rewardMultiplier = _roundController.RoundWinner == crpgPeer.SpawnTeamThisRound!.Side || isValorousPlayer
                ? crpgPeer.RewardMultiplier + 1
                : 1;

            crpgPeer.RewardMultiplier = Math.Min(rewardMultiplier, 5);
        }
    }

    private bool IsHappyHour()
    {
        var happyHours = CrpgServerConfiguration.ServerHappyHours;
        if (happyHours == null)
        {
            return false;
        }

        TimeSpan timeOfDay = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, happyHours.Item3).TimeOfDay;
        if (timeOfDay < happyHours.Item1 || happyHours.Item2 < timeOfDay)
        {
            if (_lastRewardDuringHappyHours)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new CrpgNotification
                {
                    Type = CrpgNotificationType.Announcement,
                    Message = "Happy hours ended!",
                });
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }

            _lastRewardDuringHappyHours = false;
            return false;
        }

        if (!_lastRewardDuringHappyHours)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new CrpgNotification
            {
                Type = CrpgNotificationType.Announcement,
                Message = "It's happy hours time! Experience gain is increased by 50%.",
            });
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }

        _lastRewardDuringHappyHours = true;
        return true;
    }

    /// <summary>Valorous players are the top X% of the round of the defeated team.</summary>
    private HashSet<PlayerId> GetValorousPlayers(NetworkCommunicator[] networkPeers,
        Dictionary<PlayerId, PeriodStats> allPeriodStats, bool rewardMultiplierEnabled)
    {
        if (_roundController == null || !rewardMultiplierEnabled)
        {
            return new HashSet<PlayerId>();
        }

        var defeatedTeamPlayersWithRoundScore = new List<(PlayerId playerId, int score)>();
        foreach (var networkPeer in networkPeers)
        {
            var missionPeer = networkPeer.GetComponent<MissionPeer>();
            var crpgPeer = networkPeer.GetComponent<CrpgPeer>();
            if (missionPeer == null || crpgPeer == null)
            {
                continue;
            }

            if (crpgPeer.SpawnTeamThisRound?.Side == _roundController.RoundWinner.GetOppositeSide())
            {
                var playerId = networkPeer.VirtualPlayer.Id;
                int roundScore = allPeriodStats.TryGetValue(playerId, out var s) ? s.Score : 0;
                defeatedTeamPlayersWithRoundScore.Add((playerId, roundScore));
            }
        }

        int numberOfPlayersToGiveValour = (int)(0.2f * defeatedTeamPlayersWithRoundScore.Count);
        Debug.Print($"Giving valour to {numberOfPlayersToGiveValour} out of the {defeatedTeamPlayersWithRoundScore.Count} players in the defeated team");
        return defeatedTeamPlayersWithRoundScore
            .OrderByDescending(p => p.score)
            .Take(numberOfPlayersToGiveValour)
            .Select(p => p.playerId)
            .ToHashSet();
    }

    private CrpgCharacterRating GetNewRating(CrpgPeer crpgPeer)
    {
        if (!_characterRatings.TryGetValue(crpgPeer.Peer.Id, out var rating))
        {
            return crpgPeer.User!.Character.Rating;
        }

        // Values are clamped in case there is an issue in the rating algorithm.
        return new CrpgCharacterRating
        {
            Value = MathF.Clamp(rating.Rating, -100_000, 100_000),
            Deviation = MathF.Clamp(rating.RatingDeviation, -100_000, 100_000),
            Volatility = MathF.Clamp(rating.Volatility, -100_000, 100_000),
        };
    }

    private IList<CrpgUserDamagedItem> BreakItems(CrpgPeer crpgPeer, float roundDuration)
    {
        List<CrpgUserDamagedItem> brokenItems = new();
        foreach (var equippedItem in crpgPeer.User!.Character.EquippedItems)
        {
            var mbItem = Game.Current.ObjectManager.GetObject<ItemObject>(equippedItem.UserItem.BaseItemId);
            if (_random.NextDouble() >= _constants.ItemBreakChance)
            {
                continue;
            }

            int repairCost = (int)(mbItem.Value * roundDuration * _constants.ItemRepairCostPerSecond);
            brokenItems.Add(new CrpgUserDamagedItem
            {
                UserItemId = equippedItem.UserItem.Id,
                RepairCost = repairCost,
            });
        }

        return brokenItems;
    }

    private void SetStatistics(CrpgUserUpdate userUpdate, NetworkCommunicator networkPeer,
        Dictionary<PlayerId, PeriodStats> allPeriodStats)
    {
        if (allPeriodStats.TryGetValue(networkPeer.VirtualPlayer.Id, out var peerPeriodStats))
        {
            userUpdate.Statistics = new CrpgCharacterStatistics
            {
                Kills = peerPeriodStats.Kills,
                Deaths = peerPeriodStats.Deaths,
                Assists = peerPeriodStats.Assists,
                PlayTime = peerPeriodStats.PlayTime,
            };
        }
    }

    private void SendRewardToPeers(IList<UpdateCrpgUserResult> updateResults,
        Dictionary<int, CrpgPeer> crpgPeerByUserId, HashSet<PlayerId> valorousPlayerIds)
    {
        foreach (var updateResult in updateResults)
        {
            if (!crpgPeerByUserId.TryGetValue(updateResult.User.Id, out var crpgPeer))
            {
                Debug.Print($"Unknown user with id '{updateResult.User.Id}'");
                continue;
            }

            var networkPeer = crpgPeer.GetNetworkPeer();

            crpgPeer.User = updateResult.User;
            if (crpgPeer.User.Character.ForTournament && !CrpgFeatureFlags.IsEnabled(CrpgFeatureFlags.FeatureTournament))
            {
                KickHelper.Kick(networkPeer, DisconnectType.KickedByHost, "tournament_only");
                continue;
            }

            GameNetwork.BeginModuleEventAsServer(networkPeer);
            GameNetwork.WriteMessage(new CrpgRewardUser
            {
                Reward = updateResult.EffectiveReward,
                Valour = valorousPlayerIds.Contains(networkPeer.VirtualPlayer.Id),
                RepairCost = updateResult.RepairedItems.Sum(r => r.RepairCost),
                BrokeItemIds = updateResult.RepairedItems.Where(r => r.Broke).Select(r => r.ItemId).ToList(),
            });
            GameNetwork.EndModuleEventAsServer();
        }
    }

    private void SendErrorToPeers(Dictionary<int, CrpgPeer> crpgPeerByUserId)
    {
        foreach (var crpgPeer in crpgPeerByUserId.Values)
        {
            GameNetwork.BeginModuleEventAsServer(crpgPeer.GetNetworkPeer());
            GameNetwork.WriteMessage(new CrpgRewardError());
            GameNetwork.EndModuleEventAsServer();
        }
    }
}
