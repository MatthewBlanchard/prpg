﻿using Crpg.Module.Common;
using Crpg.Module.Helpers;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.PlayerServices;

namespace Crpg.Module.Modes.Duel;

internal class CrpgDuelSpawningBehavior : CrpgSpawningBehaviorBase
{
    private readonly CrpgDuelServer _duelServer;

    public CrpgDuelSpawningBehavior(CrpgConstants constants, CrpgDuelServer duelServer)
        : base(constants)
    {
        UpdatedPlayerPreferredArenaOnce = new HashSet<PlayerId>();
        IsSpawningEnabled = true;
        _duelServer = duelServer;
    }

    public HashSet<PlayerId> UpdatedPlayerPreferredArenaOnce { get; private set; }

    public override void OnTick(float dt)
    {
        if (IsSpawningEnabled && _spawnCheckTimer.Check(Mission.CurrentTime))
        {
            SpawnAgents();
        }

        base.OnTick(dt);
    }

    protected override bool IsPlayerAllowedToSpawn(NetworkCommunicator networkPeer)
    {
        MissionPeer missionPeer = networkPeer.GetComponent<MissionPeer>();
        return missionPeer.Culture != null
               && missionPeer.Representative is DuelMissionRepresentative
               && missionPeer.SpawnTimer.Check(Mission.CurrentTime);
    }

    protected override bool IsRoundInProgress()
    {
        return Mission.CurrentState == Mission.State.Continuing;
    }

    protected override void OnPeerSpawned(MissionPeer peer)
    {
        base.OnPeerSpawned(peer);
        _ = peer.Representative; // Get initializes the representative

        if (peer.GetNetworkPeer() == null || !UpdatedPlayerPreferredArenaOnce.Add(peer.GetNetworkPeer().VirtualPlayer.Id))
        {
            return;
        }

        bool hasMount = peer.ControlledAgent.SpawnEquipment[EquipmentIndex.Horse].Item != null;
        bool isRanged = peer.ControlledAgent.SpawnEquipment.HasWeaponOfClass(WeaponClass.Bolt) || peer.ControlledAgent.SpawnEquipment.HasWeaponOfClass(WeaponClass.Arrow);
        TroopType troopType = hasMount ? TroopType.Cavalry : (isRanged ? TroopType.Ranged : TroopType.Infantry);
        GameNetwork.BeginModuleEventAsServer(peer.GetNetworkPeer());
        GameNetwork.WriteMessage(new CrpgUpdateDuelArenaType { PlayerTroopType = troopType });
        GameNetwork.EndModuleEventAsServer();

        // Sets the preferred arena server side.
        List<KeyValuePair<MissionPeer, TroopType>> peersAndSelections = (List<KeyValuePair<MissionPeer, TroopType>>)ReflectionHelper.GetField(_duelServer, "_peersAndSelections")!;
        for (int i = 0; i < peersAndSelections.Count; i++)
        {
            if (peersAndSelections[i].Key == peer)
            {
                peersAndSelections[i] = new KeyValuePair<MissionPeer, TroopType>(peer, troopType);
                return;
            }
        }
    }
}
