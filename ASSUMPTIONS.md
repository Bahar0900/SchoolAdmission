# Assumptions & Design Decisions

This document outlines the architectural assumptions and design decisions made where the project specification left room for interpretation.

---

### 1. Multi-Process Concurrency & Distributed Row Locking (Bonus Requirement)
* **Question:** How to guarantee Rule 1 (*held + confirmed never exceeds totalSeats*) even if two or more backend processes run concurrently on different ports using the same database?
* **Assumption / Solution:** 
  In-memory locks (like C# `lock` or `SemaphoreSlim`) are only effective within a single process. To guarantee 100% strict concurrency control across multiple processes, instances, or containers, we implement **Pessimistic Row-Level Locking (`SELECT * FROM classes WHERE id = @classId FOR UPDATE`)** in PostgreSQL within an explicit database transaction.
  * Whenever an application is made via `POST /classes/{id}/holds`, PostgreSQL locks that specific class row exclusively.
  * Any competing requests (even from other backend processes or server instances on different ports) are queued and serialized at the database engine level.
  * Available seats are recalculated inside the locked transaction before any hold is inserted.
  * Therefore, overbooking is mathematically impossible under any concurrent load across distributed instances.

---

### 2. Class and Shift Representation
* **Specification:** *"একটা class-এ একাধিক shift থাকতে পারে। এই system-এ প্রতিটা shift আলাদা একটা 'class' হিসেবে তৈরি হবে, নিজের আলাদা seat সংখ্যা নিয়ে। যেমন 'Class 6 – Morning Shift' আর 'Class 6 – Day Shift' দুইটা আলাদা class।"*
* **Assumption:**
  * Rather than creating separate normalized relational tables for Class and Shift, each combination is created as an independent class entity where `Name` contains the full descriptive title (e.g., `"Class 6 - Morning Shift"`).
  * This matches the API schema directly (`{ "name": "Class 6 - Morning Shift", "totalSeats": 120 }`).

---

### 3. Student Identity & Role Separation (No-Login Architecture)
* **Specification:** *"Login লাগবে না। Page-এর উপরে একটা ঘরে শিক্ষার্থীর জন্ম নিবন্ধন নম্বর লেখা যাবে। UI এটাকেই শিক্ষার্থীর পরিচয় হিসেবে ব্যবহার করবে।"*
* **Assumption:**
  * Since no authentication or password system is required, School Admin features (creating a class) are accessible directly from the UI tab without requiring role credentials.
  * The student identity is passed to the backend via the `X-Student-Id` HTTP header as a 17-digit numeric string (`^\d{17}$`).
  * On the client side, the student ID is persisted in `localStorage` so that refreshing or reopening the browser instantly restores the student's active applications.

---

### 4. Scope of the "One Active Hold / Admission" Rule
* **Specification:** *"একজন শিক্ষার্থী একটা class-এ একই সময়ে একটার বেশি active আবেদন বা নিশ্চিত ভর্তি রাখতে পারবে না।"*
* **Assumption:**
  * The restriction applies per class: a student cannot hold multiple active holds or confirmed admissions for the *same* class.
  * If a student already has a `CONFIRMED` admission or an unexpired `HELD` reservation in a given class, any new hold attempt for that class is rejected with `409 Conflict`.
  * If an earlier hold in that class has `EXPIRED` or was `CANCELLED`, the student is permitted to apply again.

---

### 5. Automatic Seat Expiration (Hybrid Approach)
* **Specification:** *"মেয়াদ শেষ হলে seat নিজে থেকে ফেরত যাবে, কাউকে কোনো API call করতে হবে না।"*
* **Assumption:**
  * Database rows do not change on their own without execution. To satisfy this requirement seamlessly:
    1. **Query-time Lazy Evaluation:** Any seat calculation (`GET /classes`, `POST /classes/{id}/holds`) automatically considers holds with `status = 'HELD'` and `expiresAt <= DateTime.UtcNow` as expired and counts those seats as available immediately down to the millisecond.
    2. **Background Periodic Sweeper:** A .NET `BackgroundService` runs every 5 seconds executing a batch update to mark expired holds as `status = 'EXPIRED'`, ensuring database integrity without requiring user interaction.

---

### 6. Cancellation of Confirmed Admissions
* **Specification:** `DELETE /holds/{holdId}` is defined as "আবেদন বাতিল" (Cancel application).
* **Assumption:**
  * Only active, unexpired holds with `status == 'HELD'` can be cancelled by the student.
  * Once an admission is `CONFIRMED` and payment is finalized, it cannot be deleted via the client delete endpoint (returns `400 Bad Request`).
