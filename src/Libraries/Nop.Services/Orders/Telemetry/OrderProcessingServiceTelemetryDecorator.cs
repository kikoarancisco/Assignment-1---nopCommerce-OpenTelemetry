namespace Nop.Services.Orders.Telemetry;

using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Nop.Services.Payments;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Shipping;

public class OrderProcessingServiceTelemetryDecorator : IOrderProcessingService
{
    private readonly IOrderProcessingService _innerService;
    
    //OpenTelemetry
    private static readonly ActivitySource _activitySource = new ActivitySource("NopCommerce.Checkout.Telemetry");
    private static readonly Meter _meter = new Meter("NopCommerce.Checkout.Metrics");

    private static readonly Counter<int> _rejectedPaymentsCounter = _meter.CreateCounter<int>(
        "checkout.payments.rejected", 
        description: "Rejected payment attempts during checkout"
    );

    private static readonly Counter<int> _approvedPaymentsCounter = _meter.CreateCounter<int>(
        "checkout.payments.approved", 
        description: "Approved payment attempts during checkout"
    );

    public OrderProcessingServiceTelemetryDecorator(IOrderProcessingService innerService)
    {
        _innerService = innerService;
    }


    public async Task<PlaceOrderResult> PlaceOrderAsync(ProcessPaymentRequest processPaymentRequest)
    {
        // 1. Initialize a new OpenTelemetry Activity for this operation
        using var activity = _activitySource.StartActivity("PlaceOrder");
        
        // 2. Add relevant tags to the activity for better observability
        activity?.SetTag("checkout.store_id", processPaymentRequest.StoreId);
        activity?.SetTag("checkout.customer_id", processPaymentRequest.CustomerId);
        activity?.SetTag("checkout.order_guid", processPaymentRequest.OrderGuid);
        activity?.SetTag("checkout.order_total", processPaymentRequest.OrderTotal);
        activity?.SetTag("checkout.payment_method", processPaymentRequest.PaymentMethodSystemName);

    try
        {
            // Called the actual PlaceOrderAsync method of the inner service
            var result = await _innerService.PlaceOrderAsync(processPaymentRequest);

        if (result.Success)
        {
            activity?.SetTag("checkout.result", "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
            
            // Increment the approved payments counter with a tag for the payment method if the order was placed successfully
            _approvedPaymentsCounter.Add(1, new KeyValuePair<string, object>("payment_method", processPaymentRequest.PaymentMethodSystemName));
        }
        else
        {
            activity?.SetTag("checkout.result", "failure");
            
            var errorMsg = result.Errors != null ? string.Join("; ", result.Errors) : "Unknown error";
            activity?.SetTag("checkout.error_message", errorMsg);
            
            //Grafana will consider any status code other than "Ok" as an error, so we set it to "Error" to make sure it shows up in our dashboards
            activity?.SetStatus(ActivityStatusCode.Error, errorMsg);

            _rejectedPaymentsCounter.Add(1, new KeyValuePair<string, object>("payment_method", processPaymentRequest.PaymentMethodSystemName));
        }
            
            return result;
        }
        catch (Exception)
        {
            // Capture any unexpected exceptions, log them in the activity, and mark the activity as an error
            activity?.SetStatus(ActivityStatusCode.Error, "Exceção ao processar pedido");
            throw; 
        }
    }

public Task CheckOrderStatusAsync(Order order) => _innerService.CheckOrderStatusAsync(order);

    public Task UpdateOrderTotalsAsync(UpdateOrderParameters updateOrderParameters) => _innerService.UpdateOrderTotalsAsync(updateOrderParameters);

    public Task DeleteOrderAsync(Order order) => _innerService.DeleteOrderAsync(order);

    public Task<IEnumerable<string>> ProcessNextRecurringPaymentAsync(RecurringPayment recurringPayment, ProcessPaymentResult paymentResult = null) => _innerService.ProcessNextRecurringPaymentAsync(recurringPayment, paymentResult);

    public Task<IList<string>> CancelRecurringPaymentAsync(RecurringPayment recurringPayment) => _innerService.CancelRecurringPaymentAsync(recurringPayment);

    public Task<bool> CanCancelRecurringPaymentAsync(Customer customerToValidate, RecurringPayment recurringPayment) => _innerService.CanCancelRecurringPaymentAsync(customerToValidate, recurringPayment);

    public Task<bool> CanRetryLastRecurringPaymentAsync(Customer customer, RecurringPayment recurringPayment) => _innerService.CanRetryLastRecurringPaymentAsync(customer, recurringPayment);

    public Task ShipAsync(Shipment shipment, bool notifyCustomer) => _innerService.ShipAsync(shipment, notifyCustomer);

    public Task ReadyForPickupAsync(Shipment shipment, bool notifyCustomer) => _innerService.ReadyForPickupAsync(shipment, notifyCustomer);

    public Task DeliverAsync(Shipment shipment, bool notifyCustomer) => _innerService.DeliverAsync(shipment, notifyCustomer);

    public bool CanCancelOrder(Order order) => _innerService.CanCancelOrder(order);

    public Task CancelOrderAsync(Order order, bool notifyCustomer) => _innerService.CancelOrderAsync(order, notifyCustomer);

    public bool CanMarkOrderAsAuthorized(Order order) => _innerService.CanMarkOrderAsAuthorized(order);

    public Task MarkAsAuthorizedAsync(Order order) => _innerService.MarkAsAuthorizedAsync(order);

    public Task<bool> CanCaptureAsync(Order order) => _innerService.CanCaptureAsync(order);

    public Task<IList<string>> CaptureAsync(Order order) => _innerService.CaptureAsync(order);

    public bool CanMarkOrderAsPaid(Order order) => _innerService.CanMarkOrderAsPaid(order);

    public Task MarkOrderAsPaidAsync(Order order) => _innerService.MarkOrderAsPaidAsync(order);

    public Task<bool> CanRefundAsync(Order order) => _innerService.CanRefundAsync(order);

    public Task<IList<string>> RefundAsync(Order order) => _innerService.RefundAsync(order);

    public bool CanRefundOffline(Order order) => _innerService.CanRefundOffline(order);

    public Task RefundOfflineAsync(Order order) => _innerService.RefundOfflineAsync(order);

    public Task<bool> CanPartiallyRefundAsync(Order order, decimal amountToRefund) => _innerService.CanPartiallyRefundAsync(order, amountToRefund);

    public Task<IList<string>> PartiallyRefundAsync(Order order, decimal amountToRefund) => _innerService.PartiallyRefundAsync(order, amountToRefund);

    public bool CanPartiallyRefundOffline(Order order, decimal amountToRefund) => _innerService.CanPartiallyRefundOffline(order, amountToRefund);

    public Task PartiallyRefundOfflineAsync(Order order, decimal amountToRefund) => _innerService.PartiallyRefundOfflineAsync(order, amountToRefund);

    public Task<bool> CanVoidAsync(Order order) => _innerService.CanVoidAsync(order);

    public Task<IList<string>> VoidAsync(Order order) => _innerService.VoidAsync(order);

    public bool CanVoidOffline(Order order) => _innerService.CanVoidOffline(order);

    public Task VoidOfflineAsync(Order order) => _innerService.VoidOfflineAsync(order);

    public Task<IList<string>> ReOrderAsync(Order order) => _innerService.ReOrderAsync(order);

    public Task<bool> IsReturnRequestAllowedAsync(Order order) => _innerService.IsReturnRequestAllowedAsync(order);

    public Task<bool> ValidateMinOrderSubtotalAmountAsync(IList<ShoppingCartItem> cart) => _innerService.ValidateMinOrderSubtotalAmountAsync(cart);

    public Task<bool> ValidateMinOrderTotalAmountAsync(IList<ShoppingCartItem> cart) => _innerService.ValidateMinOrderTotalAmountAsync(cart);

    public Task<bool> IsPaymentWorkflowRequiredAsync(IList<ShoppingCartItem> cart, bool? useRewardPoints = null) => _innerService.IsPaymentWorkflowRequiredAsync(cart, useRewardPoints);

    public Task<DateTime?> GetNextPaymentDateAsync(RecurringPayment recurringPayment) => _innerService.GetNextPaymentDateAsync(recurringPayment);

    public Task<int> GetCyclesRemainingAsync(RecurringPayment recurringPayment) => _innerService.GetCyclesRemainingAsync(recurringPayment);

    public Task<ProcessPaymentRequest> GetProcessPaymentRequestAsync() => _innerService.GetProcessPaymentRequestAsync();

    public Task SetProcessPaymentRequestAsync(ProcessPaymentRequest processPaymentRequest, bool useNewOrderGuid = false) => _innerService.SetProcessPaymentRequestAsync(processPaymentRequest, useNewOrderGuid);
}