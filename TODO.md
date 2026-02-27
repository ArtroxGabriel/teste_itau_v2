# Project Roadmap - Itau Compra Programada

## User Stories (Backlog)

- [ ] **US01: Product Adhesion & Management**: Join/leave the "Top Five" product and manage monthly contribution.
- [ ] **US02: Admin Recommendation Basket**: Manage "Top Five" basket (exactly 5 stocks, 100% total).
- [ ] **US03: Automated Purchase Motor**: Execute consolidated purchases on days 5, 15, and 25.
- [ ] **US04: Portfolio Rebalancing**: Rebalance client portfolios when basket changes or proportions drift.
- [ ] **US05: B3 Data Integration**: Parse `COTAHIST` files to get closing prices. (In Progress)
- [ ] **US06: Tax Reporting (Kafka)**: Calculate IR Dedo-duro and Profit Tax and publish to Kafka.
- [ ] **US07: Client Dashboard**: View portfolio, average prices, and detailed profitability (P/L).

## Current Task
- [x] Initial Environment Setup
- [x] Domain Entities Implementation
- [x] Infrastructure Setup (MySQL + EF Core)
- [x] B3 COTAHIST Parser
- [ ] Purchase Engine
- [ ] Rebalancing Logic
- [ ] Kafka Integration
- [ ] API Development
