﻿namespace Crpg.Domain.Entities.ActivityLogs;

public enum ActivityLogType
{
    UserCreated,
    UserDeleted,
    UserRenamed,
    UserRewarded,
    ItemBought,
    ItemSold,
    ItemBroke,
    ItemUpgraded,
    CharacterCreated,
    CharacterDeleted,
    CharacterRespecialized,
    CharacterRetired,
    CharacterRewarded,
    ServerJoined,
    ChatMessageSent,
    TeamHit,
}
