# Chester Meat Matrix

A web application that runs a wholesale meat business end to end: orders, payments, preparation, delivery, warehouse stock, staff management and reporting.

It was designed for **Chester Group Wholesale Meat** (Congella, Umbilo, KwaZulu-Natal) as an improved version of their existing "Meat Matrix" system. The goals are fewer human errors, at least 95% stock accuracy, and a simpler daily workflow for staff.

This is a solo project, built as part of my Applications Development Project 3 (ADPA301) course.

## Features

### Orders and payment
- **Place order:** customers choose the items they want and place an order
- **Pay for goods:** card payments through Stripe (test mode)

### Order preparation
- **Select order:** a sales rep selects an order and forwards it for preparation
- **Prepare order:** the order is prepared for shipment
- **Weigh delivery product:** the total weight of the order is checked
- **Dispatch order:** a checker validates and loads the ordered items
- **Create invoice:** an invoice is created for every order

### Delivery
- **Assign routes:** routes are assigned to each driver, including the order of stops
- **Schedule delivery:** drivers are alerted to deliveries they must make
- **Weigh truck:** the truck weight is checked against the planned weight
- **Deliver the order:** the driver delivers the parcel to the customer
- **Receive the order:** the customer receives and validates the ordered goods
- **Return goods:** the driver returns goods in the event of a delivery error
- **Pick up order:** in-store customers collect their parcel from the warehouse

### Warehouse stock management
- **Record stock placement:** warehouse staff record where stock is placed
- **Remove stock:** sold stock is removed from the system
- **Bind pallets:** products on the same pallet are grouped and tracked as one unit
- **Group pallets:** pallets of the same product are grouped into a single batch
- **Issue / transfer stock:** stock is moved from one warehouse location to another
- **Query stock:** shows where an item is and its movement history

### Receiving new stock
- **Order new stock:** new meat products are ordered from suppliers
- **Record new stock:** the warehouse receiver records the new stock
- **Assign storage:** the receiver records where the new stock is placed
- **Return damaged stock:** subpar stock is returned to the supplier

### Stock taking
- **Create stock take session:** the warehouse manager starts a session for a warehouse or storage location
- **Assign stock counters:** warehouse employees are assigned to count stock
- **Generate stock count sheet:** the system lists the items to count
- **Identify stock variances:** shortages and overages are displayed
- **Investigate variances and approve corrections:** the manager reviews discrepancies and approves inventory corrections
- **Generate stock take report:** a report of the results and variances

### Staff management
- **Staff work schedule:** managers create shifts, days off and overtime
- **Assign staff to tasks:** managers assign tasks based on skills, availability and workload
- **Track staff attendance:** clock-in and clock-out times, breaks and absences
- **Staff performance tracking:** tasks completed, accuracy rates and feedback
- **Manage staff leave:** staff submit leave requests and managers approve or deny them

### Equipment maintenance
- **Report truck issue:** drivers report problems with their truck
- **Book maintenance:** managers book truck maintenance

### Reporting and analytics
- **Sales and revenue reports:** create and view reports and performance analysis for the business

## Quick Tour by Role

Each type of user sees only their own pages. Log in as a role to see its part of the system.

| Role | What to try |
|------|-------------|
| Customer | Place an order, pay, and track it |
| Sales rep | Select an order and forward it for preparation |
| Checker | Weigh, validate and dispatch an order, then create the invoice |
| Router | Assign delivery routes to drivers |
| Driver | View deliveries, deliver an order, report a truck issue |
| Receiver | Order, record and place new stock |
| Warehouse staff | Record, move, pallet and query stock |
| Manager | Schedules, tasks, leave, stock takes and truck maintenance |
| Admin | Company-wide reports and performance analysis |

## Built With

- ASP.NET MVC (.NET Framework 4.7.2)
- C# and Razor views
- Entity Framework 6
- SQL Server (Azure SQL Database)
- Stripe API for payments
- OpenRouteService API for routes
- Visual Studio 2022

## Getting Started

### What you need

- Visual Studio 2022 with the **ASP.NET and web development** workload
- A SQL Server database
- A Stripe account (free test keys are enough)
- An OpenRouteService API key (optional, for route planning)

### Setup

1. Clone this repository and open `ChesterMeatMatrix.sln` in Visual Studio.
2. Passwords and API keys are **not** stored in this repository. Create these two files in the project folder (next to `Web.config`):

**`ConnectionStrings.secret.config`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<connectionStrings>
  <add name="ChesterDbContext"
       connectionString="Server=YOUR_SERVER;Initial Catalog=YOUR_DATABASE;User ID=YOUR_USER;Password=YOUR_PASSWORD;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

**`AppSecrets.secret.config`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<appSettings>
  <add key="StripeSecretKey" value="YOUR_STRIPE_SECRET_KEY" />
  <add key="StripePublishableKey" value="YOUR_STRIPE_PUBLISHABLE_KEY" />
</appSettings>
```

3. Replace the `YOUR_...` values with your own details.
4. Press **F5** to run the app.

## Deployment

The application is deployment-ready and can be hosted on **Azure App Service** with an **Azure SQL Database**. It was configured and tested for Azure deployment.

It is not currently hosted online. I am a student running on limited Azure credits, and keeping the app and database running would use them up. The full source code and setup instructions are available here, and I can deploy a live version on request.

## Security Note

The two `.secret.config` files are listed in `.gitignore`, so they are never uploaded to GitHub. Never commit real passwords or API keys.

## Project Status

All planned features are complete and working.

## Author

Built by Thabiso Gabela.
