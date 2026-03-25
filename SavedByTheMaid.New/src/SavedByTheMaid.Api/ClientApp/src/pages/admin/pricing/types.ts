export interface PriceMultiplier {
  id: number;
  name: string;
  description: string | null;
  conditionType: number;
  factor: number;
  minValue: number | null;
  maxValue: number | null;
  appliesToTime: boolean;
  appliesToPrice: boolean;
  serviceTypeId: number | null;
  serviceType: { id: number; name: string } | null;
  displayOrder: number;
  isActive: boolean;
}

export interface RecurrenceDiscount {
  id: number;
  recurrenceType: number;
  discountPercent: number;
  isActive: boolean;
}

export interface ConditionType {
  value: number;
  label: string;
  description: string;
}

export const CONDITION_TYPES: ConditionType[] = [
  { value: 0, label: 'Square Footage', description: 'Multiplier based on property size (sq ft)' },
  { value: 1, label: 'Dirt Level', description: 'Multiplier based on cleaning intensity needed' },
  { value: 2, label: 'Has Pets', description: 'Adjustment for homes with pets' },
  { value: 3, label: 'First Time', description: 'First-time cleaning surcharge' },
  { value: 4, label: 'Floor Level', description: 'Multiplier based on floor number' },
  { value: 5, label: 'No Elevator', description: 'Surcharge for no elevator access' },
  { value: 6, label: 'Extra Rooms', description: 'Additional rooms multiplier' },
];

export const RECURRENCE_TYPES = [
  { value: 0, label: 'One-time' },
  { value: 1, label: 'Weekly' },
  { value: 2, label: 'Bi-Weekly' },
  { value: 3, label: 'Monthly' },
];
