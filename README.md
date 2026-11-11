# School Admission System (বিদ্যালয় ভর্তি ব্যবস্থা)

একটি আধুনিক, স্কেলেবল এবং হাই-কনকারেন্সি সাপোর্টেড অনলাইন স্কুল ভর্তি প্ল্যাটফর্ম। সিস্টেমটি **"আগে আবেদন করলে আগে পাবেন" (First-Come, First-Served)** নীতিতে কাজ করে এবং উচ্চ ট্রাফিকের মধ্যেও ডাটাবেস লেভেলে ১০০% ওভারবুকিং প্রতিরোধ নিশ্চিত করে।

---

## 🛠️ প্রযুক্তি স্ট্যাক (Tech Stack)

* **Backend:** C# (.NET 8 Web API), PostgreSQL 16
* **Database ORM:** Entity Framework Core 8 (Code-First Migrations)
* **Concurrency Locking:** PostgreSQL Pessimistic Row-Level Locking (`SELECT ... FOR UPDATE`)
* **Frontend:** React 18, TypeScript, Vite, Tailwind CSS, Lucide Icons
* **Containerization:** Docker & Docker Compose

---

## 🚀 এক ক্লিকে সম্পূর্ণ অ্যাপ চালানোর নির্দেশাবলী (Docker Compose)

আপনার কম্পিউটারে শুধু **Docker** এবং **Docker Desktop** চালু থাকা প্রয়োজন। অন্য কোনো রানটাইম (.NET বা Node.js) ইন্সটল করার প্রয়োজন নেই।

```bash
# ১. রিপোজিটরির রুটে কমান্ডটি চালান:
docker compose up --build
```

কনটেইনারগুলো চালু হলে নিচের লিংকে প্রবেশ করুন:

| সার্ভিস | URL | বিবরণ |
|---|---|---|
| **Frontend UI** | [http://localhost:3000](http://localhost:3000) | অভিভাবকদের আবেদন ও অ্যাডমিন প্যানেল |
| **Backend API** | [http://localhost:8080](http://localhost:8080) | কোর RESTful API সার্ভিস |
| **PostgreSQL** | `localhost:5432` | পারসিস্টেন্ট ডাটাবেস ভলিউম |

> **ডাটা পারসিস্টেন্স:** `docker compose down` করার পর পুনরায় `docker compose up` করলেও সমস্ত ক্লাস এবং ভর্তির ডাটা অক্ষত থাকবে (`postgres_data` ভলিউমের মাধ্যমে)।

---

## 🧪 ব্যাকএন্ড অটোমেটেড টেস্ট চালানোর নিয়ম (Automated Tests)

সিস্টেমে ইউনিট টেস্ট, কন্ট্রাক্ট টেস্ট এবং হাই-কনকারেন্সি রেস কন্ডিশন ভেরিফিকেশন টেস্ট অন্তর্ভুক্ত রয়েছে:

```bash
# টেস্ট চালানোর কমান্ড:
dotnet test backend/SchoolAdmission.sln
```

### প্রধান টেস্টসমূহ:
1. **ConcurrencyTests.cs:**
   * একই সাথে ৫০ জন শিক্ষার্থীর মাধ্যমে মাত্র ৫টি আসনের জন্য একযোগে রিকোয়েস্ট পাঠিয়ে প্রমাণ করা হয়েছে যে মোট ৫টি সিটের বেশি কোনোভাবেই হোল্ড হয় না (বাকি ৪৫ জন `409 Conflict` পায়)।
2. **ValidationAndContractTests.cs:**
   * ১৭ ডিজিটের জন্ম নিবন্ধন নম্বর ভ্যালিডেশন (`X-Student-Id`).
   * এক শিক্ষার্থীর হোল্ড অন্য শিক্ষার্থী কনফার্ম করতে না পারা (`403 Forbidden`).
   * একাধিকবার কনফার্মেশন রিকোয়েস্ট পাঠালেও সিট সংখ্যা একবারই বৃদ্ধি পাওয়া (Idempotency).
3. **HoldLifecycleAndExpiryTests.cs:**
   * আবেদন বাতিল করলে সিট তৎক্ষণাৎ পুনরায় খালি হওয়া।
   * মেয়াদের পর স্বয়ংক্রিয়ভাবে সিট উন্মুক্ত হওয়া এবং এক্সপায়ার্ড সিট কনফার্ম করতে না পারা।

---

## ⚙️ এনভায়রনমেন্ট ভ্যারিয়েবল কনফিগারেশন

| ভ্যারিয়েবল | ডিফল্ট মান | বিবরণ |
|---|---|---|
| `DATABASE_URL` | `Host=db;Port=5432;Database=school_admission;...` | PostgreSQL ডাটাবেস কানেকশন স্ট্রিং |
| `HOLD_DURATION_SECONDS` | `120` (২ মিনিট) | আসন সাময়িক সংরক্ষণের সময়কাল (সেকেন্ডে) |
| `PORT` | `8080` | ব্যাকএন্ড সার্ভারের পোর্ট |
| `VITE_API_BASE_URL` | `http://localhost:8080` | ফ্রন্টএন্ড থেকে ব্যাকএন্ড এপিআই এর URL |

---

## 📡 API Endpoints Overview

| # | Method | Path | Header | বর্ণনা |
|---|---|---|---|---|
| 1 | `POST` | `/classes` | - | নতুন ক্লাস বা শিফট তৈরি |
| 2 | `GET` | `/classes` | - | সকল ক্লাসের তালিকা ও সিট পরিসংখ্যান |
| 3 | `GET` | `/classes/{classId}` | - | নির্দিষ্ট ক্লাসের লাইভ সিট তথ্য |
| 4 | `POST` | `/classes/{classId}/holds` | `X-Student-Id` | সিট সাময়িক সংরক্ষণ (Hold) |
| 5 | `GET` | `/holds/{holdId}` | - | আবেদনের বর্তমান অবস্থা |
| 6 | `POST` | `/holds/{holdId}/confirm` | `X-Student-Id` | ভর্তি ফি জমা ও নিশ্চিতকরণ |
| 7 | `DELETE` | `/holds/{holdId}` | `X-Student-Id` | আবেদন বাতিলকরণ (২৪৪ No Content) |
| 8 | `GET` | `/me/holds` | `X-Student-Id` | এই শিক্ষার্থীর সকল আবেদন |

---

## 💡 বিশেষ বৈশিষ্ট্যসমূহ (Key Features)

* **Multi-Instance Concurrency (Bonus):** একাধিক ব্যাকএন্ড প্রসেস ভিন্ন ভিন্ন পোর্টে চললেও ডাটাবেস লেভেলের `SELECT FOR UPDATE` পেসিমিস্টিক লকিংয়ের মাধ্যমে ওভারবুকিং প্রতিরোধ নিশ্চিত করা হয়েছে।
* **Live Updates Without Page Reload:** প্রতি ৩ সেকেন্ড পর পর স্বয়ংক্রিয় পোলিংয়ের মাধ্যমে সিট সংখ্যা রিয়েল-টাইমে আপডেট হয়।
* **Zero-Reload Countdown Timer:** মেয়াদ শেষ হওয়ার সাথে সাথে ব্রাউজারে রিফ্রেশ ছাড়াই স্ট্যাটাস `EXPIRED` হয়ে যায় এবং পেমেন্ট বাটন ডিসেবল হয়ে যায়।
* **Local Persistence:** ব্রাউজার রিলোড বা বন্ধ করে আবার খুললেও শিক্ষার্থীর ১৭ ডিজিটের পরিচয় ও চলমান আবেদন অক্ষত থাকে।
