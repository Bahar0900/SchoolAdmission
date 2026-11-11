# AI Usage Disclosure (AI_USAGE.md)

This document details the usage of Artificial Intelligence tools during the development of the **School Admission System** as required by Section 2.7 of the assignment specification.

---

## 1. AI Tool Used
* **Primary Tool:** Google Antigravity / Gemini 3.8 Flash
* **Role of AI:** Pair-programming assistant, initial code scaffolding, boilerplate generation, and syntax assistance for .NET 8 Web API, Entity Framework Core, React 18 TypeScript components, and Docker Compose configurations.

---

## 2. My Role
While AI was used to accelerate coding and generate initial templates, all core architecture decisions, testing criteria, bug catching, and business rule corrections were strictly driven by me:

1. **Mandated Code-First Architecture:**  
   AI initially leaned towards direct SQL table definitions. I explicitly intervened with the requirement to enforce **Entity Framework Core Code-First Migrations** (`db.Database.MigrateAsync()`) so that database schemas are strictly managed through application code and automatically migrate upon container startup.

2. **Resolved Multi-Class Business Logic Ambiguity:**  
   The project brief left room for interpretation regarding whether a student could hold or confirm seats across multiple classes. I analyzed the real-world impact and directed the logic:  
   * A student may temporarily hold seats across different shifts/classes during the application window.  
   * However, once **any** admission is `CONFIRMED`, all other active holds belonging to that student in other classes must be automatically cancelled and released back to the general pool, preventing seat monopolization.

3. **Security & Confidentiality Compliance:**  
   I ensured strict compliance with the assignment's rule regarding confidentiality by instructing that the provided assignment PDF remain completely ignored by Git and never published to the public GitHub repository.

---

## 3. Specific Errors Made by AI & How I Identified and Resolved Them

| # | AI Mistake / Oversight | How I Caught It | Corrective Action Taken |
|---|---|---|---|
| **1** | **Docker Platform Build Failure (`EBADPLATFORM`)**<br>AI scaffolded Vite with Windows-specific native binaries (`@rolldown/binding-win32-x64-msvc`), which broke the Docker Linux Alpine build. | I ran `docker compose up --build` in my terminal and detected the container compilation failure: `npm error code EBADPLATFORM: wanted win32, actual linux`. | Instructed AI to standardize to stable Vite 5.4 and React 18, clean the npm package lock, and configure a resilient multi-stage Node/Nginx Dockerfile. |
| **2** | **Root Endpoint 404 & Windows IPv6 Localhost Hang**<br>The AI hardcoded `http://localhost:8080` in the frontend API client. On Windows systems with Docker Desktop/WSL2, `localhost` resolves to IPv6 `::1`, which was intercepted by `wslrelay` and caused browser connection hangs. | When testing the UI in my browser, I noticed the page failed to connect and reported: *"clicking your given link localhost 3000 not showing any page"* and *"সার্ভারের সাথে সংযোগ স্থাপন করা যাচ্ছে না"*. | Diagnosed the IPv6 conflict and updated the frontend API client (`client.ts`) with smart host detection that routes requests directly to IPv4 `127.0.0.1:8080`, eliminating the hang completely. |
| **3** | **Hold Duration Discrepancy**<br>AI initially set the hold duration to 10 minutes (600 seconds) for manual testing, but the problem description mentioned 2 minutes. | I audited the PDF specifications and asked whether everything strictly aligns with the assignment rules. | Verified Section 2.5 Rule 2 (`HOLD_DURATION_SECONDS` with default 600) and ensured full configurability via environment variables so the evaluator can test any custom duration seamlessly. |
