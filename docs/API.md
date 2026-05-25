# Metavix API — Reference

**Base URL:** `/api`  
**Auth:** JWT Bearer token. Returned in the login/register response body and sent automatically via HTTP-Only cookie on subsequent requests.  
**Content-Type:** `application/json` for all requests and responses.

---

## Table of Contents

- [Auth](#auth)
  - [POST /auth/login](#post-authlogin)
  - [POST /auth/register/patient](#post-authregisterpatient)
  - [POST /auth/register/doctor](#post-authregisterdoctor)
- [Doctor](#doctor)
  - [GET /doctor/get-profile/{doctorId}](#get-doctorget-profiledoctorid)
  - [GET /doctor/{doctorId}/get-all-patients](#get-doctordoctoridget-all-patients)
  - [GET /doctor/{doctorId}/get-patient/{patientId}](#get-doctordoctoridget-patientpatientid)
  - [GET /doctor/{doctorId}/patients/{patientId}/profile](#get-doctordoctoridpatientspatientidprofile)
  - [GET /doctor/{doctorId}/patients/{patientId}/records/daily](#get-doctordoctoridpatientspatientidrecordsdaily)
  - [GET /doctor/{doctorId}/patients/{patientId}/records/lab](#get-doctordoctoridpatientspatientidrecordslab)
  - [GET /doctor/requests/pending/{doctorId}](#get-doctorrequestspendingdoctorid)
  - [POST /doctor/requests/{requestId}/accept](#post-doctorrequestsrequestidaccept)
  - [POST /doctor/requests/{requestId}/reject](#post-doctorrequestsrequestidreject)
  - [POST /doctor/requests/{requestId}/unlink](#post-doctorrequestsrequestidunlink)
- [Patient](#patient)
  - [GET /patient/get-all-doctors](#get-patientget-all-doctors)
  - [GET /patient/{patientId}/get-linked-doctors](#get-patientpatientidget-linked-doctors)
  - [POST /patient/requests-link](#post-patientrequests-link)
  - [POST /patient/requests/{requestId}/revoke](#post-patientrequestsrequestidrevoke)
  - [GET /patient/{patientId}/profile](#get-patientpatientidprofile)
  - [PATCH /patient/{patientId}/profile](#patch-patientpatientidprofile)
  - [GET /patient/{patientId}/resumen](#get-patientpatientidresumen)
  - [POST /patient/{patientId}/records/daily](#post-patientpatientidrecordsdaily)
  - [GET /patient/{patientId}/get-all/records/daily](#get-patientpatientidget-allrecordsdaily)
  - [GET /patient/{patientId}/record/daily/{recordId}](#get-patientpatientidrecorddailyrecordid)
  - [POST /patient/{patientId}/records/lab](#post-patientpatientidrecordslab)
  - [GET /patient/{patientId}/get-all/records/lab](#get-patientpatientidget-allrecordslab)
  - [GET /patient/{patientId}/records/lab/{recordId}](#get-patientpatientidrecordslabrecordid)
  - [PUT /patient/{patientId}/insulin-dm1/profile](#put-patientpatientidinsulindm1profile)
  - [GET /patient/{patientId}/insulin-dm1/profile](#get-patientpatientidinsulindm1profile)
  - [POST /patient/{patientId}/insulin-dm1/records](#post-patientpatientidinsulindm1records)
  - [GET /patient/{patientId}/insulin-dm1/records](#get-patientpatientidinsulindm1records)
  - [GET /patient/{patientId}/insulin-dm1/records/{recordId}](#get-patientpatientidinsulindm1recordsrecordid)
  - [DELETE /patient/{patientId}/insulin-dm1/records/{recordId}](#delete-patientpatientidinsulindm1recordsrecordid)
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
    "userId": "9de34f00-...",
    "patientId": "7cb12a11-...",
    "expiresAt": "2026-05-21T00:00:00Z",
    "email": "doctor@example.com",
    "role": "Doctor",
    "fullName": "John Doe"
  }
}
```

> The JWT access token is delivered via an HTTP-Only `access_token` cookie, not in the response body.
> `patientId` is only set when `role` is `"Patient"`. It is `null` for Doctor and Admin accounts.

---

### POST /auth/register/patient

Registers a new Patient account.

**Authentication:** None (public)  
**Rate limit:** `register` policy applies

**Request Body**

| Field     | Type   | Required | Description   |
|-----------|--------|----------|---------------|
| firstName | string | Yes      | First name    |
| lastName  | string | Yes      | Last name     |
| email     | string | Yes      | Email address |
| password  | string | Yes      | Password      |

```json
{
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane@example.com",
  "password": "secure456"
}
```

**Responses**

| Code | Description                               |
|------|-------------------------------------------|
| 201  | User created — `Location` header included |
| 400  | Validation error                          |
| 409  | Email already in use                      |
| 429  | Too many requests                         |

**Response Body (201)**

```json
{
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "jane@example.com",
    "role": "Patient"
  }
}
```

> The JWT access token is delivered via an HTTP-Only `access_token` cookie, not in the response body.

---

### POST /auth/register/doctor

Registers a new Doctor account.

**Authentication:** None (public)  
**Rate limit:** `register` policy applies

**Request Body**

| Field     | Type   | Required | Description   |
|-----------|--------|----------|---------------|
| firstName | string | Yes      | First name    |
| lastName  | string | Yes      | Last name     |
| email     | string | Yes      | Email address |
| password  | string | Yes      | Password      |

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@clinic.com",
  "password": "secure789"
}
```

**Responses**

| Code | Description                               |
|------|-------------------------------------------|
| 201  | User created — `Location` header included |
| 400  | Validation error                          |
| 409  | Email already in use                      |
| 429  | Too many requests                         |

**Response Body (201)**

```json
{
  "data": {
    "userId": "9de34f00-4421-4710-b3fc-1a963f44bca1",
    "email": "john@clinic.com",
    "role": "Doctor"
  }
}
```

> The JWT access token is delivered via an HTTP-Only `access_token` cookie, not in the response body.

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

### GET /doctor/{doctorId}/patients/{patientId}/profile

Returns the full profile of a patient linked to the doctor.

**Authentication:** JWT — role `Doctor` required. Link must be `Accepted`.

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| doctorId  | guid | Doctor's user ID  |
| patientId | guid | Patient's user ID |

**Responses**

| Code | Description                   |
|------|-------------------------------|
| 200  | Patient profile returned      |
| 403  | No accepted link exists       |
| 404  | Patient not found             |

**Response Body (200):** Same shape as `GET /patient/{patientId}/profile`.

---

### GET /doctor/{doctorId}/patients/{patientId}/records/daily

Returns all daily records of a patient linked to the doctor.

**Authentication:** JWT — role `Doctor` required. Link must be `Accepted`.

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| doctorId  | guid | Doctor's user ID  |
| patientId | guid | Patient's user ID |

**Responses**

| Code | Description                          |
|------|--------------------------------------|
| 200  | List of daily records (may be empty) |
| 403  | No accepted link exists              |

**Response Body (200):** Same shape as `GET /patient/{patientId}/get-all/records/daily`.

---

### GET /doctor/{doctorId}/patients/{patientId}/records/lab

Returns all lab results of a patient linked to the doctor.

**Authentication:** JWT — role `Doctor` required. Link must be `Accepted`.

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| doctorId  | guid | Doctor's user ID  |
| patientId | guid | Patient's user ID |

**Responses**

| Code | Description                           |
|------|---------------------------------------|
| 200  | List of lab results (may be empty)    |
| 403  | No accepted link exists               |

**Response Body (200):** Same shape as `GET /patient/{patientId}/get-all/records/lab`.

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

| Field              | Type                  | Required | Description                                         |
|--------------------|-----------------------|----------|-----------------------------------------------------|
| recordDate         | date                  | Yes      | Date of the record (`YYYY-MM-DD`)                   |
| recordTime         | time                  | No       | Time of the record (`HH:mm:ss` or `HH:mm:ss.fff`)   |
| systolicPressure   | int                   | No       | Systolic blood pressure (mmHg)                      |
| diastolicPressure  | int                   | No       | Diastolic blood pressure (mmHg)                     |
| heartRate          | int                   | No       | Heart rate (bpm)                                    |
| weightKg           | decimal               | No       | Weight in kilograms                                 |
| waistCm            | int                   | No       | Waist circumference in centimeters                  |
| notes              | string                | No       | Free-text notes                                     |
| glucoseReadings    | GlucoseReading[]      | No       | List of blood glucose readings for the day          |

**GlucoseReading object**

| Field       | Type   | Required | Description                                                |
|-------------|--------|----------|------------------------------------------------------------|
| readingType | int    | Yes      | Meal context — see enum below                              |
| valueMgDl   | int    | Yes      | Blood glucose value (mg/dL)                                |
| time        | time   | No       | Time of reading (`HH:mm:ss`)                               |
| foods       | string | No       | Foods consumed at this reading                             |

**GlucoseReadingType enum**

| Value | Name           |
|-------|----------------|
| 0     | Fasting        |
| 1     | PostBreakfast  |
| 2     | PreLunch       |
| 3     | PostLunch      |
| 4     | PreDinner      |
| 5     | PostDinner     |
| 6     | Snack          |
| 7     | Overnight      |

```json
{
  "recordDate": "2026-05-20",
  "recordTime": "08:30:00",
  "systolicPressure": 120,
  "diastolicPressure": 80,
  "heartRate": 72,
  "weightKg": 75.5,
  "waistCm": 88,
  "notes": "Día normal",
  "glucoseReadings": [
    { "readingType": 0, "valueMgDl": 95, "time": "07:30:00", "foods": null },
    { "readingType": 1, "valueMgDl": 140, "time": "09:00:00", "foods": "avena con fruta" }
  ]
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
    "recordDate": "20/05/2026",
    "recordTime": "08:30:00.000",
    "systolicPressure": 120,
    "diastolicPressure": 80,
    "heartRate": 72,
    "weightKg": 75.5,
    "waistCm": 88,
    "notes": "Día normal",
    "createdAt": "2026-05-20T08:31:00Z",
    "glucoseReadings": [
      { "id": "ff001122-...", "readingType": 0, "valueMgDl": 95, "time": "07:30:00.000", "foods": null },
      { "id": "ff001133-...", "readingType": 1, "valueMgDl": 140, "time": "09:00:00.000", "foods": "avena con fruta" }
    ]
  }
}
```

> `recordDate` is returned as `"dd/MM/yyyy"`. `recordTime` is returned as `"HH:mm:ss.fff"`.

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

### GET /patient/{patientId}/profile

Returns the patient's own profile data.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description        |
|-----------|------|--------------------|
| patientId | guid | Patient's entity ID |

**Responses**

| Code | Description         |
|------|---------------------|
| 200  | Profile returned    |
| 403  | Access denied       |
| 404  | Patient not found   |

**Response Body (200)**

```json
{
  "data": {
    "id": "7cb12a11-...",
    "firstName": "Jane",
    "lastName": "Smith",
    "email": "jane@example.com",
    "phone": null,
    "dateOfBirth": "1990-03-15",
    "heightCm": 165.0,
    "gender": "Female",
    "isPregnant": false,
    "diabetesType": "DM2",
    "medicalRecordNumber": "MRN-0042",
    "createdAt": "2026-05-18T10:00:00Z"
  }
}
```

---

### PATCH /patient/{patientId}/profile

Updates the patient's self-managed profile fields. Only fields provided in the request body are updated — omitted fields are left unchanged.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description        |
|-----------|------|--------------------|
| patientId | guid | Patient's entity ID |

**Request Body** *(all fields optional)*

| Field      | Type    | Description                          |
|------------|---------|--------------------------------------|
| isPregnant | bool    | Pregnancy status                     |
| heightCm   | decimal | Height in centimeters                |
| phone      | string  | Contact phone number                 |

```json
{
  "isPregnant": true
}
```

**Responses**

| Code | Description               |
|------|---------------------------|
| 200  | Profile updated — returns updated profile |
| 403  | Access denied             |
| 404  | Patient not found         |

**Response Body (200):** Same shape as [GET /patient/{patientId}/profile](#get-patientpatientidprofile).

---

### GET /patient/{patientId}/resumen

Returns a clinical summary for the patient — their profile and the most recent value for each tracked metric.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |

**Responses**

| Code | Description              |
|------|--------------------------|
| 200  | Clinical summary returned |
| 403  | Access denied            |
| 404  | Patient not found        |

**Response Body (200)**

```json
{
  "data": {
    "perfil": {
      "nombre": "Jane Smith",
      "tipoDiabetes": "DM2",
      "embarazada": false,
      "sexo": "Female"
    },
    "metricas": {
      "glucosaAyuno":        { "valor": 98.0, "fecha": "2026-05-20" },
      "presionSistolica":    { "valor": 120,  "fecha": "2026-05-20" },
      "presionDiastolica":   { "valor": 80,   "fecha": "2026-05-20" },
      "frecuenciaCardiaca":  { "valor": 72,   "fecha": "2026-05-20" },
      "peso":                { "valor": 75.5, "fecha": "2026-05-20" },
      "estaturasCm":         { "valor": null, "fecha": null },
      "imc":                 { "valor": null, "fecha": null },
      "cintura":             { "valor": 88,   "fecha": "2026-05-20" },
      "hba1c":               { "valor": 6.5,  "fecha": "2026-05-18" },
      "colesterolTotal":     { "valor": 185,  "fecha": "2026-05-18" },
      "colesterolLdl":       { "valor": 110,  "fecha": "2026-05-18" },
      "colesterolHdl":       { "valor": 50,   "fecha": "2026-05-18" },
      "trigliceridos":       { "valor": 130,  "fecha": "2026-05-18" },
      "creatinina":          { "valor": 0.9,  "fecha": "2026-05-18" },
      "bun":                 { "valor": 14,   "fecha": "2026-05-18" }
    }
  }
}
```

---

### PUT /patient/{patientId}/insulin-dm1/profile

Creates or updates the insulin dosing profile for a DM1 patient (upsert).

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |

**Request Body**

| Field           | Type    | Required | Description                              |
|-----------------|---------|----------|------------------------------------------|
| insulinName     | string  | No       | Name of the insulin used                 |
| ric             | decimal | No       | Ratio insulina:carbohidratos             |
| sensitivityFactor | int   | No       | Insulin sensitivity factor (mg/dL per unit) |
| targetGlucose   | int     | No       | Target blood glucose (mg/dL)             |
| doctorName      | string  | No       | Treating doctor's name                   |
| doctorPhone     | string  | No       | Treating doctor's phone                  |

```json
{
  "insulinName": "Humalog",
  "ric": 15.0,
  "sensitivityFactor": 50,
  "targetGlucose": 100,
  "doctorName": "Dr. John Doe",
  "doctorPhone": "+52-664-000-0000"
}
```

**Responses**

| Code | Description             |
|------|-------------------------|
| 200  | Profile saved           |

**Response Body (200)**

```json
{
  "data": {
    "id": "aabbcc-...",
    "patientId": "7cb12a11-...",
    "insulinName": "Humalog",
    "ric": 15.0,
    "sensitivityFactor": 50,
    "targetGlucose": 100,
    "doctorName": "Dr. John Doe",
    "doctorPhone": "+52-664-000-0000",
    "createdAt": "2026-05-10T09:00:00Z",
    "updatedAt": "2026-05-20T11:00:00Z"
  }
}
```

---

### GET /patient/{patientId}/insulin-dm1/profile

Returns the insulin dosing profile for a DM1 patient.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |

**Responses**

| Code | Description          |
|------|----------------------|
| 200  | Profile returned     |
| 404  | Profile not found    |

**Response Body (200):** Same object as [Upsert Insulin Profile (200)](#put-patientpatientidinsulindm1profile).

---

### POST /patient/{patientId}/insulin-dm1/records

Adds a new insulin dosing record for the patient.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |

**Request Body**

| Field           | Type    | Required | Description                           |
|-----------------|---------|----------|---------------------------------------|
| recordDate      | date    | Yes      | Date of the record (`YYYY-MM-DD`)     |
| glucoseBefore   | int     | No       | Blood glucose before meal (mg/dL)     |
| glucoseAfter    | int     | No       | Blood glucose after meal (mg/dL)      |
| totalCarbs      | decimal | No       | Total carbohydrates consumed (grams)  |
| doseApplied     | decimal | No       | Insulin dose applied (units)          |
| mealDescription | string  | No       | Description of the meal               |
| howIFelt        | string  | No       | Patient's subjective feeling          |

```json
{
  "recordDate": "2026-05-20",
  "glucoseBefore": 130,
  "glucoseAfter": 95,
  "totalCarbs": 45.0,
  "doseApplied": 3.0,
  "mealDescription": "Lunch — rice and chicken",
  "howIFelt": "Good, no hypoglycemia"
}
```

**Responses**

| Code | Description                                  |
|------|----------------------------------------------|
| 201  | Record created — `Location` header included  |
| 400  | Validation error                             |

**Response Body (201)**

```json
{
  "data": {
    "id": "ddeeff-...",
    "patientId": "7cb12a11-...",
    "recordDate": "2026-05-20",
    "glucoseBefore": 130,
    "glucoseAfter": 95,
    "totalCarbs": 45.0,
    "doseApplied": 3.0,
    "mealDescription": "Lunch — rice and chicken",
    "howIFelt": "Good, no hypoglycemia",
    "createdAt": "2026-05-20T13:05:00Z"
  }
}
```

---

### GET /patient/{patientId}/insulin-dm1/records

Returns all insulin dosing records for a patient.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |

**Responses**

| Code | Description                             |
|------|-----------------------------------------|
| 200  | List of insulin records (may be empty)  |
| 404  | Patient not found                       |

**Response Body (200):** Array of the same object as [Add Insulin Record (201)](#post-patientpatientidinsulindm1records).

---

### GET /patient/{patientId}/insulin-dm1/records/{recordId}

Returns a single insulin dosing record by its ID.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |
| recordId  | guid | Insulin record ID |

**Responses**

| Code | Description       |
|------|-------------------|
| 200  | Record returned   |
| 404  | Record not found  |

**Response Body (200):** Same object as [Add Insulin Record (201)](#post-patientpatientidinsulindm1records).

---

### DELETE /patient/{patientId}/insulin-dm1/records/{recordId}

Deletes a single insulin dosing record.

**Authentication:** JWT — role `Patient` required

**Path Parameters**

| Name      | Type | Description       |
|-----------|------|-------------------|
| patientId | guid | Patient's user ID |
| recordId  | guid | Insulin record ID |

**Request Body:** None

**Responses**

| Code | Description       |
|------|-------------------|
| 204  | Record deleted    |
| 404  | Record not found  |

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
