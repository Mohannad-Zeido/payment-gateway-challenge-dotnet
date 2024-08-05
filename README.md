# Coding Challenge

This repo is my submission to the Engineering Assessment.

## Design Decisions:

The main discussion point here is the use of the IPaymentService interface for the Bank Client. This allows for future 
expansion where we would want to integrate with more acquiring banks. Having this interface means the main application
doesn't need to know the details of each bank. All it cares about is for this given request I expect a certain result. 
The actual implementation of this interface is the code that knows what needs to happen on the banks side to get the result
we need. It acts as the translator between our domain logic and the bank's logic.

## How to Test:

Run the following command in the root directory of the project
```shell
dotnet test
```

## How To Run:

### Step 1: Run the fake bank server (Run the following command in the root directory of this repo)
```shell
  docker-compose up
```

### Step 2: Navigate to the PaymentGateway.Api directory and run

``` shell
    dotnet run
```

### Step 3: The Swagger documentation of this API can be found [here](https://localhost:7092/swagger/index.html)

### Step 4.1: To process a successful payment please use the following JSON in the request body
```json
{
    "expiry_year" : 2025,
    "expiry_month" : 4,
    "amount" : 100,
    "card_number" : "2222405343248877",
    "currency" : "GBP",
    "cvv" : "123"
}
```

### Step 4.2: To process a declined payment please use the following JSON in the request body
```json
{
    "expiry_year" : 2026,
    "expiry_month" : 1,
    "amount" : 60000,
    "card_number" : "2222405343248112",
    "currency" : "USD",
    "cvv" : "456"
}
```

### Step 4.3: To send an invalid request please use the following JSON in the request body
```json
{
    "expiry_year" : 2026,
    "expiry_month" : 1,
    "amount" : 60000,
    "card_number" : "2222405343248112",
    "currency" : "USD",
    "cvv" : "46"
}
```
NOTE: there are more validation errors to explore. Please refer to [this](https://github.com/cko-recruitment/.github/tree/beta?tab=readme-ov-file#processing-a-payment) and try them out