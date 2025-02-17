import {
  type ItemFlat,
  ItemType,
  ItemFieldFormat,
  WeaponClass,
  ItemFieldCompareRule,
} from '@/models/item';
import { AggregationView, type AggregationConfig } from '@/models/item-search';

const size = 1000;

export const aggregationsConfig: AggregationConfig = {
  id: {
    sort: 'term',
    size,
    conjunction: false,
    view: AggregationView.Checkbox,
    chosen_filters_on_top: false,
    hidden: true,
  },
  modId: {
    sort: 'term',
    size,
    conjunction: false,
    view: AggregationView.Checkbox,
    chosen_filters_on_top: false,
    hidden: true,
  },
  type: {
    sort: 'term',
    size,
    conjunction: false,
    view: AggregationView.Radio,
    chosen_filters_on_top: false,
  },
  price: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Less,
    width: 200,
  },
  culture: {
    sort: 'term',
    size,
    view: AggregationView.Checkbox,
    conjunction: false,
    chosen_filters_on_top: false,
  },
  tier: {
    size,
    conjunction: false,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    chosen_filters_on_top: false,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  requirement: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Requirement,
    compareRule: ItemFieldCompareRule.Less,
  },
  weight: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Less,
  },
  flags: {
    sort: 'term',
    size,
    conjunction: false,
    view: AggregationView.Checkbox,
    chosen_filters_on_top: false,
    format: ItemFieldFormat.List,
    width: 160,
  },

  // Armor
  bodyArmor: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  headArmor: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  armArmor: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  legArmor: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },

  // Mount
  bodyLength: {
    size,
    format: ItemFieldFormat.Number,
    view: AggregationView.Range,
  },
  chargeDamage: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  maneuver: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  speed: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  hitPoints: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  mountFamilyType: {
    size,
    view: AggregationView.Checkbox,
    sort: 'term',
    conjunction: false,
    chosen_filters_on_top: false,
  },

  // Mount armor
  mountArmor: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  mountArmorFamilyType: {
    size,
    view: AggregationView.Checkbox,
    sort: 'term',
    conjunction: false,
    chosen_filters_on_top: false,
  },

  // Weapon
  weaponClass: {
    size,
    sort: 'term',
    conjunction: false,
    view: AggregationView.Radio,
    chosen_filters_on_top: false,
  },
  weaponUsage: {
    size,
    sort: 'term',
    conjunction: false,
    view: AggregationView.Checkbox,
    chosen_filters_on_top: false,
    hidden: true,
  },
  length: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  handling: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  thrustSpeed: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  thrustDamage: {
    size,
    view: AggregationView.Range,
    hide_zero_doc_count: true,
    conjunction: false,
    format: ItemFieldFormat.Damage,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  thrustDamageType: {
    size,
    sort: 'term',
    conjunction: false,
    view: AggregationView.Checkbox,
    chosen_filters_on_top: false,
  },
  swingSpeed: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  swingDamage: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Damage,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  swingDamageType: {
    size,
    sort: 'term',
    conjunction: false,
    view: AggregationView.Checkbox,
    chosen_filters_on_top: false,
  },

  // Throw/Bow/Xbow
  missileSpeed: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  accuracy: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },

  // Bow/Xbow
  reloadSpeed: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  aimSpeed: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },

  // Arrows/Bolts/Thrown
  damage: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Damage,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  damageType: {
    size,
    sort: 'term',
    conjunction: false,
    view: AggregationView.Checkbox,
    chosen_filters_on_top: false,
  },
  stackAmount: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  stackWeight: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Less,
  },

  // SHIELD
  shieldSpeed: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  shieldDurability: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
  shieldArmor: {
    size,
    view: AggregationView.Range,
    format: ItemFieldFormat.Number,
    compareRule: ItemFieldCompareRule.Bigger,
  },
};

export const aggregationsKeysByItemType: Partial<Record<ItemType, Array<keyof ItemFlat>>> =
  // prettier-ignore
  {
  [ItemType.HeadArmor]: [
    'culture',
    'flags',
    'weight',
    'headArmor',
    'price',
  ],
  [ItemType.BodyArmor]: [
    'culture',
    'flags',
    'weight',
    'bodyArmor',
    'armArmor',
    'legArmor',
    'price',
  ],
  [ItemType.LegArmor]: [
    'culture',
    'weight',
    'legArmor',
    'price',
  ],
  [ItemType.HandArmor]: [
    'culture',
    'flags',
    'weight',
    'armArmor',
    'price',
  ],
  [ItemType.ShoulderArmor]: [
    'culture',
    'flags',
    'weight',
    'bodyArmor',
    'armArmor',
    'price',
  ],

  [ItemType.Mount]: [
    'culture',
    'mountFamilyType',
    'chargeDamage',
    'maneuver',
    'speed',
    'hitPoints',
    'price',
  ],
  [ItemType.MountHarness]: [
    'culture',
    'mountArmorFamilyType',
    'weight',
    'mountArmor',
    'price',
  ],

  [ItemType.Crossbow]: [
    'flags',
    'weight',
    'damage',
    'accuracy',
    'missileSpeed',
    'reloadSpeed',
    'aimSpeed',
    'requirement',
    'price',
  ],
  [ItemType.Bow]: [
    'flags',
    'weight',
    'damage',
    'accuracy',
    'missileSpeed',
    'reloadSpeed',
    'aimSpeed',
    'price',
  ],

  [ItemType.Arrows]: [
    'damageType',
    'damage',
    'stackWeight',
    'stackAmount',
    'price',
  ],
  [ItemType.Bolts]: [
    'damageType',
    'damage',
    'stackWeight',
    'stackAmount',
    'price',
  ],

  [ItemType.Thrown]: [
    'damage',
    'missileSpeed',
    'stackWeight',
    'stackAmount',
    'price',
  ],

  [ItemType.Shield]: [
    'flags',
    'weight',
    'length',
    'shieldSpeed',
    'shieldDurability',
    'shieldArmor',
    'price',
  ],

  [ItemType.OneHandedWeapon]: [
    'weaponUsage',
    'flags',
    'weight',
    'length',
    'handling',
    'thrustDamage',
    'thrustSpeed',
    'swingDamage',
    'swingSpeed',
    'price',
  ],
  [ItemType.TwoHandedWeapon]: [
    'flags',
    'weight',
    'length',
    'handling',
    'thrustDamage',
    'thrustSpeed',
    'swingDamage',
    'swingSpeed',
    'price',
  ],
  [ItemType.Polearm]: [
    'weaponUsage',
    'flags',
    'weight',
    'length',
    'handling',
    'thrustDamage',
    'thrustSpeed',
    'swingDamage',
    'swingSpeed',
    'price',
  ],

  // banners are all the same, no need for aggregation
  [ItemType.Banner]: [
    'flags',
    'weight',
    'culture',
    'price',
  ],
};

export const aggregationsKeysByWeaponClass: Partial<Record<WeaponClass, Array<keyof ItemFlat>>> =
  // prettier-ignore
  {
  [WeaponClass.OneHandedSword]: [
    'weaponUsage',
    'weight',
    'length',
    'handling',
    'thrustDamage',
    'thrustSpeed',
    'swingDamage',
    'swingSpeed',
    'price',
  ],
  [WeaponClass.OneHandedAxe]: [
    'flags',
    'weight',
    'weaponUsage',
    'length',
    'handling',
    'swingDamage',
    'swingSpeed',
    'price',
  ],
  [WeaponClass.Mace]: [
    'flags',
    'weight',
    'length',
    'handling',
    'swingDamage',
    'swingSpeed',
    'price',
  ],
  [WeaponClass.Dagger]: [
    'length',
    'weight',
    'handling',
    'thrustDamage',
    'thrustSpeed',
    'swingDamage',
    'swingSpeed',
    'price',
  ],
  [WeaponClass.TwoHandedAxe]: [
    'flags',
    'weight',
    'length',
    'handling',
    'swingDamage',
    'swingSpeed',
    'price',
  ],
  [WeaponClass.TwoHandedMace]: [
    'flags',
    'weight',
    'length',
    'handling',
    'swingDamage',
    'swingSpeed',
    'price',
  ],

  [WeaponClass.Javelin]: [
    'flags',
    'damage',
    'missileSpeed',
    'stackWeight',
    'stackAmount',
    'price',
  ],

  [WeaponClass.ThrowingAxe]: [
    'flags',
    'damage',
    'missileSpeed',
    'stackWeight',
    'stackAmount',
    'price',
  ],

  [WeaponClass.ThrowingKnife]: [
    'damage',
    'weaponUsage',
    'missileSpeed',
    'stackWeight',
    'stackAmount',
    'price',
  ],
};
