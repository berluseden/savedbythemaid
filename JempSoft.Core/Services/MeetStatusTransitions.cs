using JempSoft.Core.Common;
using JempSoft.Core.Errors;

namespace JempSoft.Core.Services
{
    /// <summary>
    /// Validates state transitions for ServiceOrder lifecycle.
    /// ServiceOrder uses boolean flags (IsActive, IsComplete, IsPayed) rather than an enum.
    /// Valid lifecycle: Active -> Complete -> Paid (or Active -> Inactive for cancellation).
    /// </summary>
    public static class MeetStatusTransitions
    {
        public static Result ValidateComplete(bool isActive, bool isComplete)
        {
            if (!isActive)
                return Result.Failure(ServiceOrderErrors.CannotCompleteInactive());
            if (isComplete)
                return Result.Failure(ServiceOrderErrors.AlreadyComplete());
            return Result.Success();
        }

        public static Result ValidateDeactivate(bool isComplete)
        {
            if (isComplete)
                return Result.Failure(ServiceOrderErrors.CannotDeactivateCompleted());
            return Result.Success();
        }

        public static Result ValidateModifiable(bool isComplete)
        {
            if (isComplete)
                return Result.Failure(ServiceOrderErrors.CannotModifyCompleted());
            return Result.Success();
        }

        public static Result ValidatePayment(bool isActive)
        {
            if (!isActive)
                return Result.Failure(ServiceOrderErrors.CannotPayInactive());
            return Result.Success();
        }
    }
}
