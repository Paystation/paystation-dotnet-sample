# Overview

This implementation uses an iframe and AJAX to contain the entire payment journey in a single page without redirecting the user away from your website. This is a 3 party integration. On your checkout page it opens up an iframe where the user can make a payment. Once they have made the payment the iframe closes.

To determine when the transaction is complete, the status of the transaction is polled. This works on two levels. Firstly between the browser and your web server, and secondly between your webserver and Paystation. The browser uses AJAX to query the transaction details from your webserver. And in turn, your web server uses Paystation's Quick Lookup API and or listens for the Paystation POST response (callback). When the browser sees the transaction status is complete it closes the iframe and ends the purchase journey.

The purchase journey is like so:
1. User/cardholder has something in their shopping cart and has come to the checkout page, where the amount they will pay can be calculated.
2. A transaction for that amount is created in Paystation. Paystation returns a URL for the iframe to load wherein the user can enter their credit card details and make the payment.
3. The user is directed to a checkout page containing an iframe where the user can pay with their card. Meanwhile in the background there is a javascript that continually polls your server (which in turn queries Paystation) to see if the transaction has been complete.
4. After the user has submitted their payment, the javascript polling the transaction details will get a response with a completed transaction and close the iframe.

There are 3 ways that your site gets transaction information from Paystation:
1. When the user submits the amount, a request is sent to Paystation to create a transaction. This returns the transaction ID and a link to where the user can make the payment.
2. When AJAX requests are used to poll the transaction, a request is sent to paystation to lookup the status of the transaction. (Lookup.cs)
3. When the callback is received when a transaction is completed. (HomeController.Callback())

# Configuration

Your Paystation ID, Gateway ID and HMAC key

# Architecture

App_Start\RouteConfig.cs
Controllers\HomeController.cs // Has 4 endpoints. 2 views for the checkout. 1 AJAX endpoint. 1 endpoint for the Paystation POST response.
Helpers\Lookup.cs // Calls Paystation Quick Lookup API.
Helpers\Payment.cs // For creating a new transaction.
Helpers\Request.cs // Wrapper for HTTP requests that supports hmac shared secrets required for dynamic urls and talking to the APIs without needing a whitelisted IP
Models\Transaction.cs // Dummy class to store transaction data.
Scripts\paystation.js // Utility functions used for AJAXing checkout pages.
Views\Home\Index.cshtml // Checkout page.
Web.config // This is where you enter in your Paystation credentials, within <appSettings></appSettings>