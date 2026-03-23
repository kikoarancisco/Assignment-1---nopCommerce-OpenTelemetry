# Architectural Critique & Observability Strategy
**Target Flow:** Customer places an order (Checkout Pipeline)

## 1. Architecture Analysis
nopCommerce relies heavily on Dependency Injection (DI) and interface-based service contracts (e.g., `IOrderProcessingService`). This architectural choice was extremely helpful because it allowed for the interception of method calls without needing to modify the core implementation. The centralized DI registration in `NopStartup.cs` made it straightforward to inject telemetry dependencies.

The domain logic and infrastructural concerns are sometimes tightly coupled. The checkout flow spans multiple services (`OrderService`, `PaymentService`, `ShoppingCartService`), making it challenging to trace a single logical transaction seamlessly without passing an explicit context object around. 

## 2. The Change
To instrument the `PlaceOrderAsync` method, I avoided modifying the core `OrderProcessingService.cs` directly. Modifying the core business logic violates the Open/Closed Principle and risks breaking existing checkout functionalities.

**The Solution:** I introduced a **Decorator Pattern** by creating `OrderProcessingServiceTelemetryDecorator.cs`. 
* **Why it was necessary:** It allowed me to wrap the `PlaceOrderAsync` method with an OpenTelemetry `ActivitySource` and `Meter` counters, starting a span before the original method executes and catching exceptions/success states after it returns.
* **Minimizing Impact:** The only change to the existing architecture was a single line in `NopStartup.cs` to register the decorator in the DI container. The core business logic remains completely unaware of the telemetry infrastructure.

## 3. Observability Strategy & Metric Justification
I implemented two custom metrics focused on operational insights rather than generic infrastructure metrics:
1. `checkout.payments.approved` (Counter)
2. `checkout.payments.rejected` (Counter)

While standard HTTP metrics provide request duration and general 500/400 error rates, they lack business context. If a payment provider API starts failing (e.g., rejecting valid cards), the HTTP response to the user might still be a successful 200 OK (loading a "Payment Failed" UI page). My custom metrics would immediately alert an on-call engineer that the checkout pipeline is degrading from a business perspective, allowing them to act before users start abandoning their carts.

## 4. Privacy Strategy
E-commerce checkout flows inherently deal with highly sensitive Personally Identifiable Information (PII) and Payment Card Industry (PCI) data.

I chose to handle redaction at the **SDK layer** (by selective inclusion) rather than at the Collector or Storage layer. 

PII should never leave the application process. If the collector configuration fails, PII leaks into the network. By strictly extracting only safe business context (e.g., `checkout.store_id`, `checkout.payment_method`, `checkout.order_total`) and actively ignoring fields like Customer Name, Email, or Credit Card structures, we guarantee privacy by design.

## 5. Future Architectural Improvements
If I were making long-term architectural decisions for nopCommerce's observability:
1. **Event-Driven Telemetry:** I would change nopCommerce's internal `IEventPublisher` to automatically generate spans for domain events (e.g., `EntityInsertedEvent<Order>`). This would decouple telemetry completely from the service layer.
2. **Context Propagation:** I would implement a Correlation ID middleware that attaches a unique trace ID to all logs, events, and database calls, ensuring that asynchronous background tasks (like sending order confirmation emails) are linked to the parent checkout trace.

## 6. AI Usage Declaration: Oriented Learning Approach

In the development of this assignment, Generative AI (Google Gemini) was utilized strictly under an **Oriented Learning** and technical mentoring framework. 

The collaboration focused on the following architectural and debugging aspects:

* **Architectural Sounding Board:** Before writing the instrumentation code, I used the AI to discuss the tradeoffs of modifying the legacy `OrderProcessingService` directly versus using alternative design patterns. This oriented discussion led to the implementation of the **Decorator Pattern**, which safely isolated the telemetry infrastructure from the core business logic.
* **Interactive Troubleshooting:** When dealing with complex infrastructure behaviors (e.g., Prometheus data staleness rules, OpenTelemetry HTTP path appending, and Grafana PromQL label-matching restrictions), the AI was used as a diagnostic partner. Instead of just requesting working queries, the AI was prompted to explain *why* certain behaviors were occurring (like the `No data` errors in Grafana gauges), allowing me to understand the underlying mechanics of time-series databases.
* **Simulated Load Strategy:** The AI assisted in formulating the hybrid load-testing strategy, suggesting the use of `k6` for baseline traffic generation while performing manual edge-case testing (e.g., simulating payment provider rejections) to populate the telemetry signals meaningfully.

Ultimately, all architectural decisions, code implementations, and dashboard designs were my own, with the AI acting as a specialized tutor to bridge knowledge gaps in advanced observability concepts.