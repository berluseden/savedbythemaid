export {
  emailSchema,
  phoneSchema,
  zipCodeSchema,
  requiredString,
  passwordSchema,
} from './common.schema';

export {
  loginSchema,
  registerSchema,
  forgotPasswordSchema,
  changePasswordSchema,
} from './auth.schema';
export type {
  LoginFormData,
  RegisterFormData,
  ForgotPasswordFormData,
  ChangePasswordFormData,
} from './auth.schema';

export {
  zipCodeStepSchema,
  serviceStepSchema,
  detailsStepSchema,
  scheduleStepSchema,
  contactStepSchema,
  bookingSchema,
} from './booking.schema';
export type {
  ZipCodeStepData,
  ServiceStepData,
  DetailsStepData,
  ScheduleStepData,
  ContactStepData,
  BookingFormData,
} from './booking.schema';

export { contactSchema } from './contact.schema';
export type { ContactFormData } from './contact.schema';

export { profileSchema } from './profile.schema';
export type { ProfileFormData } from './profile.schema';

export {
  employeeSchema,
  serviceTypeSchema,
  cleaningPlaceSchema,
  cleaningPlaceRoomSchema,
  additionalServiceSchema,
  priceMultiplierSchema,
  recurrenceDiscountSchema,
  serviceAreaSchema,
  userSchema,
  passwordResetSchema,
} from './admin.schema';
export type {
  EmployeeFormData,
  ServiceTypeFormData,
  CleaningPlaceFormData,
  CleaningPlaceRoomFormData,
  AdditionalServiceFormData,
  PriceMultiplierFormData,
  RecurrenceDiscountFormData,
  ServiceAreaFormData,
  UserFormData,
  PasswordResetFormData,
} from './admin.schema';
