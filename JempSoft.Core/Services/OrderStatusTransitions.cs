using System.Collections.Generic;
using JempSoft.Core.Common;
using JempSoft.Core.Errors;
using JempSoft.Core.Models.Invent;

namespace JempSoft.Core.Services
{
    /// <summary>
    /// Defines valid status transitions for SalesOrder, PurchaseOrder, and TransferOrder.
    /// All three order types share the same Draft -> Open -> Completed state machine.
    /// </summary>
    public static class OrderStatusTransitions
    {
        private static readonly Dictionary<SalesOrderStatus, SalesOrderStatus[]> _salesOrderTransitions =
            new Dictionary<SalesOrderStatus, SalesOrderStatus[]>
            {
                [SalesOrderStatus.Draft] = new[] { SalesOrderStatus.Open },
                [SalesOrderStatus.Open] = new[] { SalesOrderStatus.Completed },
                [SalesOrderStatus.Completed] = new SalesOrderStatus[0]
            };

        private static readonly Dictionary<PurchaseOrderStatus, PurchaseOrderStatus[]> _purchaseOrderTransitions =
            new Dictionary<PurchaseOrderStatus, PurchaseOrderStatus[]>
            {
                [PurchaseOrderStatus.Draft] = new[] { PurchaseOrderStatus.Open },
                [PurchaseOrderStatus.Open] = new[] { PurchaseOrderStatus.Completed },
                [PurchaseOrderStatus.Completed] = new PurchaseOrderStatus[0]
            };

        private static readonly Dictionary<TransferOrderStatus, TransferOrderStatus[]> _transferOrderTransitions =
            new Dictionary<TransferOrderStatus, TransferOrderStatus[]>
            {
                [TransferOrderStatus.Draft] = new[] { TransferOrderStatus.Open },
                [TransferOrderStatus.Open] = new[] { TransferOrderStatus.Completed },
                [TransferOrderStatus.Completed] = new TransferOrderStatus[0]
            };

        // --- SalesOrder ---

        public static bool IsValid(SalesOrderStatus from, SalesOrderStatus to)
        {
            if (from == to) return true;
            return _salesOrderTransitions.ContainsKey(from) &&
                   System.Array.IndexOf(_salesOrderTransitions[from], to) >= 0;
        }

        public static Result Validate(SalesOrderStatus from, SalesOrderStatus to)
        {
            if (IsValid(from, to))
                return Result.Success();
            return Result.Failure(OrderErrors.InvalidTransition(from, to));
        }

        // --- PurchaseOrder ---

        public static bool IsValid(PurchaseOrderStatus from, PurchaseOrderStatus to)
        {
            if (from == to) return true;
            return _purchaseOrderTransitions.ContainsKey(from) &&
                   System.Array.IndexOf(_purchaseOrderTransitions[from], to) >= 0;
        }

        public static Result Validate(PurchaseOrderStatus from, PurchaseOrderStatus to)
        {
            if (IsValid(from, to))
                return Result.Success();
            return Result.Failure(OrderErrors.InvalidPurchaseOrderTransition(from, to));
        }

        // --- TransferOrder ---

        public static bool IsValid(TransferOrderStatus from, TransferOrderStatus to)
        {
            if (from == to) return true;
            return _transferOrderTransitions.ContainsKey(from) &&
                   System.Array.IndexOf(_transferOrderTransitions[from], to) >= 0;
        }

        public static Result Validate(TransferOrderStatus from, TransferOrderStatus to)
        {
            if (IsValid(from, to))
                return Result.Success();
            return Result.Failure(OrderErrors.InvalidTransferOrderTransition(from, to));
        }

        // --- Editability checks ---

        public static Result ValidateEditable(SalesOrderStatus status)
        {
            if (status == SalesOrderStatus.Completed)
                return Result.Failure(OrderErrors.CannotEditCompleted());
            return Result.Success();
        }

        public static Result ValidateEditable(PurchaseOrderStatus status)
        {
            if (status == PurchaseOrderStatus.Completed)
                return Result.Failure(OrderErrors.CannotEditCompleted());
            return Result.Success();
        }

        public static Result ValidateEditable(TransferOrderStatus status)
        {
            if (status == TransferOrderStatus.Completed)
                return Result.Failure(OrderErrors.CannotEditCompleted());
            return Result.Success();
        }

        public static Result ValidateTransferOrderEditable(bool isIssued, bool isReceived)
        {
            if (isIssued || isReceived)
                return Result.Failure(OrderErrors.TransferOrderAlreadyProcessed());
            return Result.Success();
        }
    }
}
