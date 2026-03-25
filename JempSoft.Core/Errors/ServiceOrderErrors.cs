using JempSoft.Core.Common;

namespace JempSoft.Core.Errors
{
    public static class ServiceOrderErrors
    {
        public static Error CannotDeactivateCompleted() =>
            new Error("ServiceOrder.CannotDeactivateCompleted",
                "Cannot deactivate a completed service order.");

        public static Error CannotCompleteInactive() =>
            new Error("ServiceOrder.CannotCompleteInactive",
                "Cannot complete an inactive service order.");

        public static Error CannotModifyCompleted() =>
            new Error("ServiceOrder.CannotModifyCompleted",
                "Cannot modify a completed service order.");

        public static Error AlreadyComplete() =>
            new Error("ServiceOrder.AlreadyComplete",
                "Service order is already completed.");

        public static Error CannotPayInactive() =>
            new Error("ServiceOrder.CannotPayInactive",
                "Cannot mark payment on an inactive service order.");
    }
}
