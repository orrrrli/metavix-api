# Metavix API — Reference

**Base URL:** `/api`  
**Auth:** JWT Bearer token. Returned in the login/register response body and sent automatically via HTTP-Only cookie on subsequent requests.  
**Content-Type:** `application/json` for all requests and responses.

---

## Table of Contents

- [Auth](#auth)
  - [POST /auth/login](#post-authlogin)
  - [POST /auth/register](#post-authregister)
- [Doctor](#doctor)
  - [GET /doctor/get-profile/{doctorId}](#get-doctorget-profiledoctorid)
  - [GET /doctor/{doctorId}/get-all-patients](#get-doctordoctoridget-all-patients)
  - [GET /doctor/{doctorId}/get-patient/{patientId}](#get-doctordoctoridget-patientpatientid)
  - [GET /doctor/requests/pending/{doctorId}](#get-doctorrequestspendingdoctorid)
  - [POST /doctor/requests/{requestId}/accept](#post-doctorrequestsrequestidaccept)
  - [POST /doctor/requests/{requestId}/reject](#post-doctorrequestsrequestidreject)
  - [POST /doctor/requests/{requestId}/unlink](#post-doctorrequestsrequestidunlink)
- [Patient](#patient)
  - [GET /patient/get-all-doctors](#get-patientget-all-doctors)
  - [GET /patient/{patientId}/get-linked-doctors](#get-patientpatientidget-linked-doctors)
  - [POST /patient/requests-link](#post-patientrequests-link)
  - [POST /patient/requests/{requestId}/revoke](#post-patientrequestsrequestidrevoke)
  - [POST /patient/{patientId}/records/daily](#post-patientpatientidrecordsdaily)
  - [GET /patient/{patientId}/get-all/records/daily](#get-patientpatientidget-allrecordsdaily)
  - [GET /patient/{patientId}/record/daily/{recordId}](#get-patientpatientidrecorddailyrecordid)
  - [POST /patient/{patientId}/records/lab](#post-patientpatientidrecordslab)
  - [GET /patient/{patientId}/get-all/records/lab](#get-patientpatientidget-allrecordslab)
  - [GET /patient/{patientId}/records/lab/{recordId}](#get-patientpatientidrecordslabrecordid)
- [Admin](#admin)
  - [GET /admin/logs](#get-adminlogs)
  - [GET /admin/logs/{correlationId}](#get-adminlogscorrelationid)

---

## Auth

> These endpoints are public — no JWT required. Both apply a rate limiter to prevent brute-force attacks.

---

### POST /auth/login

Authenticates a user and returns a JWT access token.

**Authentication:** None (public)  
**Rate limit:** `login` policy applies

**Request Body**

| Field    | Type   | Required | Description        |
|----------|--------|----------|--------------------|
| email    | string | Yes      | User email address |
| password | string | Yes      | User password      |

```json
{
  "email": "doctor@example.com",
  "password": "secret123"
}
```

**Responses**

| Code | Description                          |
|------|--------------------------------------|
| 200  | Login successful — returns JWT data  |
| 401  | Invalid credentials                  |
| 429  | Too many requests                    |

**Response Body (200)**

```json
{
  "data": {
    "accessToken": "eyJhbGci...",
    "expiresAt": "2026-05-21T00:00:00Z",
    "email": "doctor@example.com",
    "role": "Doctor",
    "fullName": "John Doe"
  }
}
```

---

### POST /auth/register

Registers a new user account (Doctor, Patient, or Admin).

**Authentication:** None (public)  
**Rate limit:** `register` policy applies

> **Note:** The `role` field must be sent as a valid enum value. Accepted values: `0` (Doctor), `1` (Patient), `2` (Admin).

**Request Body**

| Field     | Type   | Required | Description                            |
|-----------|--------|----------|----------------------------------------|
| firstName | string | Yes      | First name                             |
| lastName  | string | Yes      | Last name                              |
| email     | string | Yes      | Email address                          |
| password  | string | Yes      | Password                               |
| role      | int    | Yes      | `0` = Doctor, `1` = Patient, `2` = Admin |

```json
{
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane@example.com",
  "password": "secure456",
  "role": 1
}
```

**Responses**

| Code | Description                                |
|------|--------------------------------------------|
| 201  | User created — `Location` header included  |
| 400  | Validation error                           |
| 409  | Email already in use                       |
| 429  | Too many requests                          |

**Response Body (201)**

```json
{
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "jane@example.com",
    "role": "Patient",
    "token": "eyJhbGci..."
  }
}
```

---

## Doctor

> All endpoints in this group require a valid JWT with role `Doctor`.

---

### GET /doctor/get-profile/{doctorId}

Returns the full profile of a doctor.

**Authentication:** JWT — role `Doctor` required

**Path Parameters**

| Name     | Type | Description     |
|----------|------|-----------------|
| doctorId | guid | Doctor's user ID |

**Responses**

| Code | Description             |
|------|-------------------------|
| 200  | Doctor profile returned |
| 404  | Doctor not found        |

**Response Body (200)**

```json
{
  "data": {
    "id": "3fa85f64-...",
    "firstName": "John",
    "lastName": "Doe",
    "licenseNumber": "MX-12345",
    "speciality": "Endocrinology",
    "email": "john@clinic.com",
    "phone": "+52-664-000-0000",
    "isActive": true,
    "createdAt": "2026-01-15T10:00:00Z"
  }
}
```

---

### GET /doctor/{doctorId}/get-all-patients

Returns all patients linked to the doctor.

**Authentication:** JWT — role `Doctor` required

**Path Parameters**

| Name     | Type | Description      |
|----------|------|------------------|
| doctorId | guid | Doctor's user ID |

**Responses**

| Code | Description                    |
|------|--------------------------------|
| 200  | List of linked patients (may be empty) |

**Response Body (200)**

```json
{
  "data": [
    {
      "name": "Jane",
      "lastName": "Smith",
      "medicalRecordNumber": "MRN-0042"
    }
  ]
}
```

---

### GET /doctor/{doctorId}/get-patient/{patientId}

Returns a single patient linked to the doctor.

**Authentication:** JWT — role `Doctor` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| doctorId  | guid | Doctor's user ID  |
| patientId | guid | Patient's user ID |

**Responses**

| Code | Description       |
|------|-------------------|
| 200  | Patient returned  |
| 404  | Patient not found |

**Response Body (200)**

```json
{
  "data": {
    "name": "Jane",
    "lastName": "Smith",
    "medicalRecordNumber": "MRN-0042"
  }
}
```

---

### GET /doctor/requests/pending/{doctorId}

Returns all pending link requests sent by patients to this doctor.

**Authentication:** JWT — role `Doctor` required

**Path Parameters**

| Name     | Type | Description      |
|----------|------|------------------|
| doctorId | guid | Doctor's user ID |

**Responses**

| Code | Description                           |
|------|---------------------------------------|
| 200  | List of pending requests (may be empty) |

**Response Body (200)**

```json
{
  "data": [
    {
      "requestId": "3fa85f64-...",
      "patientId": "7cb12a11-...",
      "patientFirstName": "Jane",
      "patientLastName": "Smith",
      "patientEmail": "jane@example.com",
      "createdAt": "2026-05-20T14:00:00Z"
    }
  ]
}
```

---

### POST /doctor/requests/{requestId}/accept

Accepts a pending link request from a patient.

**Authentication:** JWT — role `Doctor` required

**Path Parameters**

| Name      | Type | Description          |
|-----------|------|----------------------|
| requestId | guid | Link request ID      |

**Request Body:** None

**Responses**

| Code | Description               |
|------|---------------------------|
| 200  | Request accepted          |
| 404  | Request not found         |

**Response Body (200)**

```json
{
  "data": {
    "requestId": "3fa85f64-...",
    "patientId": "7cb12a11-...",
    "doctorId": "9de34f00-...",
    "status": "Accepted",
    "createdAt": "2026-05-20T14:00:00Z"
  }
}
```

---

### POST /doctor/requests/{requestId}/reject

Rejects a pending link request from a patient.

**Authentication:** JWT — role `Doctor` required

**Path Parameters**

| Name      | Type | Description     |
|-----------|------|-----------------|
| requestId | guid | Link request ID |

**Request Body:** None

**Responses**

| Code | Description       |
|------|-------------------|
| 200  | Request rejected  |
| 404  | Request not found |

**Response Body (200):** Same shape as [accept](#post-doctorrequestsrequestidaccept) with `"status": "Rejected"`.

---

### POST /doctor/requests/{requestId}/unlink

Unlinks a patient from the doctor's panel (terminates an existing link).

**Authentication:** JWT — role `Doctor` required

**Path Parameters**

| Name      | Type | Description     |
|-----------|------|-----------------|
| requestId | guid | Link request ID |

**Request Body:** None

**Responses**

| Code | Description        |
|------|--------------------|
| 200  | Patient unlinked   |
| 404  | Request not found  |

**Response Body (200):** Same shape as [accept](#post-doctorrequestsrequestidaccept) with `"status": "Unlinked"`.

---

## Patient

> All endpoints in this group require a valid JWT with role `Patient`.

---

### GET /patient/get-all-doctors

Returns all registered doctors available for linking.

**Authentication:** JWT — role `Patient` required

**Responses**

| Code | Description                            |
|------|----------------------------------------|
| 200  | List of doctors (may be empty)         |

**Response Body (200)**

```json
{
  "data": [
    {
      "id": "9de34f00-...",
      "firstName": "John",
      "lastName": "Doe",
      "speciality": "Endocrinology",
      "email": "john@clinic.com"
    }
  ]
}
```

---

### GET /patient/{patientId}/get-linked-doctors

Returns all doctors currently linked to this patient.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |

**Responses**

| Code | Description                               |
|------|-------------------------------------------|
| 200  | List of linked doctors (may be empty)     |

**Response Body (200)**

```json
{
  "data": [
    {
      "requestId": "3fa85f64-...",
      "doctorId": "9de34f00-...",
      "doctorFirstName": "John",
      "doctorLastName": "Doe",
      "speciality": "Endocrinology",
      "email": "john@clinic.com",
      "linkedAt": "2026-05-10T08:00:00Z"
    }
  ]
}
```

---

### POST /patient/requests-link

Sends a link request from a patient to a doctor.

**Authentication:** JWT — role `Patient` required

**Request Body**

| Field     | Type | Required | Description      |
|-----------|------|----------|------------------|
| patientId | guid | Yes      | Patient's user ID |
| doctorId  | guid | Yes      | Target doctor's ID |

```json
{
  "patientId": "7cb12a11-...",
  "doctorId": "9de34f00-..."
}
```

**Responses**

| Code | Description                                         |
|------|-----------------------------------------------------|
| 201  | Request created — `Location` header included        |
| 409  | An active link request to this doctor already exists |

**Response Body (201)**

```json
{
  "data": {
    "requestId": "3fa85f64-...",
    "patientId": "7cb12a11-...",
    "doctorId": "9de34f00-...",
    "status": "Pending",
    "createdAt": "2026-05-20T14:00:00Z"
  }
}
```

---

### POST /patient/requests/{requestId}/revoke

Revokes a previously sent link request or removes an existing link from the patient's side.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description     |
|-----------|------|-----------------|
| requestId | guid | Link request ID |

**Request Body:** None

**Responses**

| Code | Description        |
|------|--------------------|
| 200  | Access revoked     |
| 404  | Request not found  |

**Response Body (200):** Same shape as [requests-link](#post-patientrequests-link) with `"status": "Revoked"`.

---

### POST /patient/{patientId}/records/daily

Adds a new daily health record for the patient.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |

> **Note:** `patientId` in the path takes precedence over the field in the body. The body field is ignored if sent.

**Request Body**

| Field            | Type    | Required | Description                        |
|------------------|---------|----------|------------------------------------|
| recordDate       | date    | Yes      | Date of the record (`YYYY-MM-DD`)  |
| recordTime       | time    | No       | Time of the record (`HH:mm:ss`)    |
| systolicPressure | int     | No       | Systolic blood pressure (mmHg)     |
| diastolicPressure| int     | No       | Diastolic blood pressure (mmHg)    |
| heartRate        | int     | No       | Heart rate (bpm)                   |
| weightKg         | decimal | No       | Weight in kilograms                |
| waistCm          | int     | No       | Waist circumference in centimeters |
| notes            | string  | No       | Free-text notes                    |

```json
{
  "recordDate": "2026-05-20",
  "recordTime": "08:30:00",
  "systolicPressure": 120,
  "diastolicPressure": 80,
  "heartRate": 72,
  "weightKg": 75.5,
  "waistCm": 88,
  "notes": "Felt fine after breakfast"
}
```

**Responses**

| Code | Description                                    |
|------|------------------------------------------------|
| 201  | Record created — `Location` header included    |
| 400  | Validation error                               |

**Response Body (201)**

```json
{
  "data": {
    "id": "aabbcc-...",
    "patientId": "7cb12a11-...",
    "recordDate": "2026-05-20",
    "recordTime": "08:30:00",
    "systolicPressure": 120,
    "diastolicPressure": 80,
    "heartRate": 72,
    "weightKg": 75.5,
    "waistCm": 88,
    "notes": "Felt fine after breakfast",
    "createdAt": "2026-05-20T08:31:00Z"
  }
}
```

---

### GET /patient/{patientId}/get-all/records/daily

Returns all daily records for a patient.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |

**Responses**

| Code | Description                             |
|------|-----------------------------------------|
| 200  | List of daily records (may be empty)    |

**Response Body (200):** Array of the same object as [Add Daily Record (201)](#post-patientpatientidrecordsdaily).

---

### GET /patient/{patientId}/record/daily/{recordId}

Returns a single daily record by its ID.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |
| recordId  | guid | Daily record ID   |

**Responses**

| Code | Description         |
|------|---------------------|
| 200  | Record returned     |
| 404  | Record not found    |

**Response Body (200):** Same object as [Add Daily Record (201)](#post-patientpatientidrecordsdaily).

---

### POST /patient/{patientId}/records/lab

Adds a new lab result record for the patient.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |

> **Note:** `patientId` in the path takes precedence over the field in the body.

**Request Body**

| Field           | Type    | Required | Description                             |
|-----------------|---------|----------|-----------------------------------------|
| sampleDate      | date    | Yes      | Date sample was taken (`YYYY-MM-DD`)    |
| hba1c           | decimal | No       | Glycated hemoglobin (%)                 |
| totalCholesterol| decimal | No       | Total cholesterol (mg/dL)               |
| ldl             | decimal | No       | LDL cholesterol (mg/dL)                 |
| hdl             | decimal | No       | HDL cholesterol (mg/dL)                 |
| triglycerides   | decimal | No       | Triglycerides (mg/dL)                   |
| creatinine      | decimal | No       | Creatinine (mg/dL)                      |
| bun             | decimal | No       | Blood urea nitrogen (mg/dL)             |
| egoProteins     | string  | No       | Urine dipstick — proteins               |
| egoGlucose      | string  | No       | Urine dipstick — glucose                |
| notes           | string  | No       | Free-text notes                         |

```json
{
  "sampleDate": "2026-05-18",
  "hba1c": 6.5,
  "totalCholesterol": 185.0,
  "ldl": 110.0,
  "hdl": 50.0,
  "triglycerides": 130.0,
  "creatinine": 0.9,
  "bun": 14.0,
  "egoProteins": "Negative",
  "egoGlucose": "Negative",
  "notes": "Fasting sample"
}
```

**Responses**

| Code | Description                                  |
|------|----------------------------------------------|
| 201  | Lab result created — `Location` header included |
| 400  | Validation error                             |

**Response Body (201)**

```json
{
  "data": {
    "id": "ccddee-...",
    "patientId": "7cb12a11-...",
    "sampleDate": "2026-05-18",
    "hba1c": 6.5,
    "totalCholesterol": 185.0,
    "ldl": 110.0,
    "hdl": 50.0,
    "triglycerides": 130.0,
    "creatinine": 0.9,
    "bun": 14.0,
    "egoProteins": "Negative",
    "egoGlucose": "Negative",
    "notes": "Fasting sample",
    "createdAt": "2026-05-18T10:00:00Z"
  }
}
```

---

### GET /patient/{patientId}/get-all/records/lab

Returns all lab result records for a patient.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |

**Responses**

| Code | Description                            |
|------|----------------------------------------|
| 200  | List of lab records (may be empty)     |

**Response Body (200):** Array of the same object as [Add Lab Result (201)](#post-patientpatientidrecordslab).

---

### GET /patient/{patientId}/records/lab/{recordId}

Returns a single lab result by its ID.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |
| recordId  | guid | Lab result ID     |

**Responses**

| Code | Description         |
|------|---------------------|
| 200  | Record returned     |
| 404  | Record not found    |

**Response Body (200):** Same object as [Add Lab Result (201)](#post-patientpatientidrecordslab).

---

## Admin

> All endpoints in this group require a valid JWT with role `Admin`.

---

### GET /admin/logs

Returns a paginated list of structured log entries with optional filters.

**Authentication:** JWT — role `Admin` required

**Query Parameters**

| Name      | Type     | Required | Default | Description                               |
|-----------|----------|----------|---------|-------------------------------------------|
| level     | string   | No       | —       | Filter by log level (`Information`, `Warning`, `Error`) |
| endpoint  | string   | No       | —       | Filter by endpoint path (partial match)   |
| userId    | string   | No       | —       | Filter by user ID                         |
| from      | datetime | No       | —       | Start of date range (ISO 8601)            |
| to        | datetime | No       | —       | End of date range (ISO 8601)              |
| page      | int      | No       | `1`     | Page number                               |
| pageSize  | int      | No       | `50`    | Results per page                          |

**Responses**

| Code | Description               |
|------|---------------------------|
| 200  | Paginated log list        |

**Response Body (200)**

```json
{
  "data": {
    "data": [
      {
        "id": 1,
        "message": "GET /api/doctor/get-profile/... responded 200",
        "level": "Information",
        "raiseDate": "2026-05-20T10:00:00Z",
        "exception": null,
        "httpMethod": "GET",
        "endpoint": "/api/doctor/get-profile/{doctorId}",
        "correlationId": "abc-123",
        "userId": "9de34f00-...",
        "role": "Doctor"
      }
    ],
    "total": 340,
    "page": 1,
    "pageSize": 50
  }
}
```

---

### GET /admin/logs/{correlationId}

Returns all log entries associated with a single request correlation ID. Useful for tracing a full request lifecycle.

**Authentication:** JWT — role `Admin` required

**Path Parameters**

| Name          | Type   | Description                  |
|---------------|--------|------------------------------|
| correlationId | string | Correlation ID of the request |

**Responses**

| Code | Description                                  |
|------|----------------------------------------------|
| 200  | All log entries for that correlation ID      |
| 404  | No logs found for this correlation ID        |

**Response Body (200)**

```json
{
  "data": [
    {
      "id": 12,
      "message": "POST /api/auth/login responded 200",
      "level": "Information",
      "raiseDate": "2026-05-20T09:45:00Z",
      "exception": null,
      "httpMethod": "POST",
      "endpoint": "/api/auth/login",
      "correlationId": "abc-123",
      "userId": null,
      "role": null
    }
  ]
}
```
