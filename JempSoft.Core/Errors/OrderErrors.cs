using JempSoft.Core.Common;
using JempSoft.Core.Models.Invent;

namespace JempSoft.Core.Errors
{
    public static class OrderErrors
    {
        public static Error InvalidTransition(SalesOrderStatus from, SalesOrderStatus to) =>
            new Error("Order.InvalidTransition",
                $"Cannot transition order status from {from} to {to}.");

        public static Error CannotEditCompleted() =>
            new Error("Order.CannotEditCompleted",
                "Cannot edit a completed order.");

        public static Error InvalidPurchaseOrderTransition(PurchaseOrderStatus from, PurchaseOrderStatus to) =>
            new Error("PurchaseOrder.InvalidTransition",
                $"Cannot transition purchase order status from {from} to {to}.");

        public static Error InvalidTransferOrderTransition(TransferOrderStatus from, TransferOrderStatus to) =>
            new Error("TransferOrder.InvalidTransition",
                $"Cannot transition transfer order status from {from} to {to}.");

        public static Error TransferOrderAlreadyProcessed() =>
            new Error("TransferOrder.AlreadyProcessed",
                "Cannot edit an open order that has already processed goods issue or goods receive.");
    }
}
