# 🛒 HotToysStore

An e-commerce website for collectible figures, Gundam models, Hot Toys, and anime merchandise built with ASP.NET Core MVC and Oracle Database.

---

## 📌 Overview

HotToysStore is a full-stack web application that allows customers to browse products, manage shopping carts, place orders, and make online payments.

The project was developed to practice enterprise software architecture, database design, and modern design patterns in ASP.NET Core.

---

## 🚀 Features

### Customer Features

* User Registration & Login
* Product Browsing
* Product Search
* Product Details Page
* Shopping Cart
* Guest Checkout
* Order Management
* Customer Profile Management

### Administration Features

* Product Management
* Category Management
* Customer Management
* Order Management
* Banner Management
* Promotion Management
* Revenue Statistics Dashboard

### Payment Features

* VNPay Integration
* Online Payment Processing
* Payment Verification

### Additional Features

* Session-based Authentication
* Shopping Cart Persistence
* Revenue Analytics
* Top Products Statistics
* Top Customers Statistics

---

## 🏗 Architecture

The project follows SOLID principles and implements multiple design patterns:

### Design Patterns

* Repository Pattern
* Unit Of Work Pattern
* Strategy Pattern
* State Pattern
* Factory Method Pattern
* Facade Pattern

### Software Principles

* SOLID Principles
* Dependency Injection
* Separation of Concerns

---

## 💻 Technology Stack

### Backend

* ASP.NET Core MVC
* C#
* Entity Framework Core
* Oracle Database

### Frontend

* HTML5
* CSS3
* JavaScript
* Razor Views
* Bootstrap

### Tools

* Visual Studio 2022
* Git
* GitHub

---

## 📂 Project Structure

```text
HotToysStore
│
├── Controllers
├── Models
├── Domain
├── Infrastructure
├── Services
├── Middleware
├── Helpers
├── Views
├── ViewComponents
├── Migrations
└── wwwroot
```

---

## ⚙️ Installation

### Clone Repository

```bash
git clone https://github.com/huudong2005/HotToysStore.git
```

### Restore Packages

```bash
dotnet restore
```

### Configure Database

Create your own:

```text
appsettings.json
```

Example:

```json
{
  "ConnectionStrings": {
    "OracleConnection": "YOUR_CONNECTION_STRING"
  }
}
```

### Apply Migrations

```bash
dotnet ef database update
```

### Run Application

```bash
dotnet run
```

---

## 📊 Database

* Oracle Database
* Entity Framework Core
* Code First Migration

---

## 🔒 Security

Sensitive files are excluded from source control:

* appsettings.json
* appsettings.Development.json

Database credentials and API secrets should never be committed to GitHub.

---

## 🎯 Learning Objectives

This project was built to gain experience with:

* ASP.NET Core MVC Development
* Oracle Database Integration
* Design Patterns
* SOLID Principles
* E-Commerce System Design
* Payment Gateway Integration

---

## 👨‍💻 Author

**Dong Ky**

GitHub: https://github.com/huudong2005

---

## 📄 License

This project is developed for educational and portfolio purposes.
