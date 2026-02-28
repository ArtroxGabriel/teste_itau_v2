# Project Roadmap - Itau Compra Programada

## User Stories (Backlog)

- [/] **US01: Product Adhesion & Management**: Join/leave the "Top Five" product and manage monthly contribution.
    - [x] Client Adhesion (Join)
    - [ ] Client Exit (Leave)
    - [ ] Update Monthly Contribution
- [ ] **US02: Admin Recommendation Basket**: Manage "Top Five" basket (exactly 5 stocks, 100% total).
- [x] **US03: Automated Purchase Motor**: Execute consolidated purchases on days 5, 15, and 25.
- [/] **US04: Portfolio Rebalancing**: Rebalance client portfolios when basket changes or proportions drift.
    - [x] Rebalance on Basket Change
    - [ ] RN-050: Rebalance on Proportion Drift (Postponed)
- [x] **US05: B3 Data Integration**: Parse `COTAHIST` files to get closing prices.
- [x] **US06: Tax Reporting (Kafka)**: Calculate IR Dedo-duro and Profit Tax and publish to Kafka.
- [x] **US07: Client Dashboard**: View portfolio, average prices, and detailed profitability (P/L).

## Technical Tasks
- [x] Initial Environment Setup
- [x] Domain Entities Implementation
- [x] Infrastructure Setup (MySQL + EF Core)
- [x] B3 COTAHIST Parser
- [x] Resiliency & Idempotency Pipeline
- [x] Refactor: ExecutePurchaseMotorCommandHandler (Break down complex logic)
- [x] Code Quality: Enforce linting and formatting
- [x] API Development: US07 Endpoints
