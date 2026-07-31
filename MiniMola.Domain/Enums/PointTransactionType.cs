using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Domain.Enums;

public enum PointTransactionType
{
    WelcomeBonus = 1,
    GameReward = 2,
    DailyReward = 3,
    FishPurchase = 4,
    DecorationPurchase = 5,
    AquariumUpgrade = 6,
    Refund = 7,
    AdminAdjustment = 8
}