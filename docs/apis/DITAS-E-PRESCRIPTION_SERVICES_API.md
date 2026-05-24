COMPACT API DOCUMENTATION OUTLINE: ARMED FORCES E-PRESCRIPTION API SERVICES

Provider:
Armed Forces Medical Services Insurance / SAKHAD

Integration Channel:
DITAS / Ministry of Health API Gateway

Portal:
eSAKHAD

Base URL:
https://apigateway.behdasht.gov.ir

Common service prefix:
POST /api/client/apim/v1/niroomosalah_sakhad/gwSakhad/{operation}

Common headers:
Content-Type: application/json
Authorization: Bearer {ditas_access_token}
X_Authorization: Bearer {armed_forces_access_token}
pid: {ditas_service_pid}
sessionId: {user_session_id}

Note:
Token and session creation operations use a reduced or different header set.

1. AUTHENTICATION SERVICES

1.1. Get DITAS Token

Purpose:
Receive DITAS gateway access token.

Method:
POST

Path:
/oauth/token

Headers:
Authorization: Basic {base64(client_id:client_secret)}
Content-Type: application/x-www-form-urlencoded

Body:
grant_type=password
username={ditas_username}
password={ditas_password}

Output:
access_token
refresh_token
scope
token_type
expires_in

1.2. Refresh DITAS Token

Purpose:
Refresh DITAS gateway token.

Method:
POST

Path:
/oauth/token

Headers:
Authorization: Basic {base64(client_id:client_secret)}
Content-Type: application/x-www-form-urlencoded

Body:
grant_type=refresh_token
refresh_token={refresh_token}

Output:
access_token
refresh_token
scope
token_type
expires_in

1.3. Get Armed Forces Access Token

Purpose:
Receive Armed Forces / SAKHAD access token.

Method:
POST

Path:
/Token

Headers:
Content-Type: application/json
ClientId: {issued_client_id}
ClientSecret: {issued_client_secret}
pid: {ditas_service_pid}
Authorization: Bearer {ditas_access_token}

Body:
{
  "username": "{provider_username}",
  "password": "{provider_password}"
}

Output:
access_token
refresh_token
token_type
scope
expires_in

1.4. Create User Session

Purpose:
Create provider user session for doctor or service provider.

Method:
POST

Path:
/fetch

Headers:
Content-Type: application/json
X_Authorization: Bearer {armed_forces_access_token}
pid: {ditas_service_pid}
Authorization: Bearer {ditas_access_token}

Body:
{
  "username": "{user_username}",
  "password": "{user_password}",
  "cid": "{center_id}"
}

Output:
sessionId
user_identity
center_info
expiration_info

Rules:
If two-step authentication is active, the first session is temporary and must be validated with the OTP flow before calling prescription services.

2. ELIGIBILITY AND IDENTITY SERVICES

2.1. Insured Person Eligibility Inquiry

Purpose:
Check patient identity and treatment entitlement.

Method:
POST

Path:
/eligible

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}"
}

Output:
nationalNumber
name
lastName
birthDate
gender
relationTypeCode
relationType
mobile
isCovered
isCoveredTitle
esarStatusType
esarStatusTitle
insuranceNumber
insurer
diseases[]
militaryStatusType
militaryStatus
memberImage

Business rules:
Normal electronic prescription should continue only when coverage is valid.
The result must be stored with provider_key = ARMED_FORCES.

3. CENTER AND DOCTOR SERVICES

3.1. Get Center Doctor List

Purpose:
Fetch doctors available in the current contracted center.

Method:
POST

Path:
/centerDoctorList

Headers:
Common headers

Body:
{}

Output:
centerCode
nomedicalSystem
name
family
specialty
section
universityDegree
medicalSystemType

3.2. Get Technical Manager List

Purpose:
Fetch technical managers of the current center.

Method:
POST

Path:
Official path must be confirmed from API package.

Headers:
Common headers

Output:
centerCode
nomedicalSystem
name
family
specialty
section
universityDegree
medicalSystemType

3.3. Get Specialty List By Name

Purpose:
Search provider specialty list.

Method:
POST

Path:
/getSpecialityListByName

Headers:
Common headers

Body:
{
  "name": "{search_text}",
  "pageNumber": 1
}

Output:
specialtyList[]
totalPage

3.4. Doctor Inquiry

Purpose:
Fetch doctor identity and contract details.

Method:
POST

Path:
/inquiryDoctor

Headers:
Common headers

Body:
{
  "nomedicalSystem": "{medical_system_number_or_member_code}",
  "medicalSystemType": "1"
}

medicalSystemType:
1 = Medical Council number
2 = Member code / no Medical Council number

Output:
centerCode
nomedicalSystem
name
family
specialty
section
universityDegree
medicalSystemType

4. PRESCRIPTION WRITING SERVICES

4.1. Get Prescription Appointment

Purpose:
Create or fetch a prescription-writing appointment/reference for a patient and doctor.

Method:
POST

Path:
/getAppointment

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "noMedicalSystem": "{doctor_medical_system_no}",
  "medicalSystemType": "1",
  "mobile": "{patient_mobile}"
}

Output:
referingCode
doctor
nomedicalSystem
medicalSystemType
academicDiscipline
grade
universityDegree

4.2. Get Prescription Visitor List

Purpose:
Fetch patient visitor list for prescription writing.

Method:
POST

Path:
/getClientListTajviz

Headers:
Common headers

Body:
{}

Output:
referingCode
nationalNumber
fullName
memberImage

4.3. Delete Prescription Appointment

Purpose:
Delete a prescription-writing appointment/reference.

Method:
POST

Path:
/deleteAppointment

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "referingCode": "{refering_code}"
}

Output:
result

4.4. Inquiry Prescription Before Submission

Purpose:
Validate prescription payload before final registration.

Method:
POST

Path:
/inquiryPrescription

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "medicalSystemType": "1",
  "nomedicalSystem": "{doctor_medical_system_no}",
  "mobile": "{patient_mobile}",
  "trackingCode": "",
  "referringCode": "{refering_code}",
  "prescriptionInputApiDeleteModelList": [],
  "prescriptionInputApiModelList": []
}

Item fields:
terminology
prescriptionTypeCode
code
description
count
consumption
consumptionInstruction
emergency
hospitalName
bulkId
otc

Output:
nationalNumber
medicalSystemType
nomedicalSystem
mobile
quotaOrganization
patientFranshiz
totalAmounts
trackingCode
hid
reject
referringCode
patientComplaintCode
patientComplaintDesc
prescriptionOutputApiModelList[]
messages[]

4.5. Register Electronic Prescription

Purpose:
Register, edit, or delete prescription items at prescription-writing level.

Method:
POST

Path:
/registerPrescription

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "medicalSystemType": "1",
  "nomedicalSystem": "{doctor_medical_system_no}",
  "mobile": "{patient_mobile}",
  "trackingCode": "{tracking_code_or_empty}",
  "referringCode": "{refering_code}",
  "reject": null,
  "patientComplaintCode": "{complaint_code}",
  "prescriptionInputApiModelList": [],
  "prescriptionInputApiDeleteModelList": []
}

Output:
nationalNumber
medicalSystemType
nomedicalSystem
mobile
quotaOrganization
patientFranshiz
totalAmounts
trackingCode
hid
reject
referringCode
patientComplaintCode
patientComplaintDesc
prescriptionOutputApiModelList[]

5. PRESCRIPTION WRITING QUERY SERVICES

5.1. Fetch Prescription Print File

Purpose:
Generate printable prescription file.

Method:
POST

Path:
/printPresentation

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "trackingCode": "{tracking_code}",
  "tajvizType": "{tajviz_type}"
}

Output:
file
fileName
fileType
fileExtension
fileBase64

5.2. Get Prescription History

Purpose:
Fetch doctor prescription history for a patient.

Method:
POST

Path:
/getHistory

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "fromDate": "{yyyyMMdd}",
  "toDate": "{yyyyMMdd}"
}

Output:
lastName
firstName
nationalNumber
trackingCode
rowNumberInMonth
prescriptionType
status
prescriptionDate
prescriptionUuid

5.3. Get Prescription History Details

Purpose:
Fetch item-level details of a prescription-history row.

Method:
POST

Path:
/getSubHistory

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "prescriptionUuid": "{prescription_uuid}"
}

Output:
prescription details
items[]
messages[]

5.4. Get Medical History By Prescribing Doctor

Purpose:
Fetch patient treatment history for a specific prescribing doctor.

Method:
POST

Path:
/medicalHistoryDoctor

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "noMedicalSystem": "{doctor_medical_system_no}",
  "medicalSystemType": "1",
  "fromDate": "{yyyyMMdd}",
  "toDate": "{yyyyMMdd}"
}

Output:
identifier
tajvizTypeDesc
referralDate
tracingCode

5.5. Get Medical History Detail

Purpose:
Fetch detail of a selected medical-history record.

Method:
POST

Path:
/medicalHistoryDoctorDetail

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "identifier": "{history_identifier}"
}

Output:
items[]
fullName
count
consumption
consumptionInstruction
emergency
presentedCount

6. DISPENSING / SERVICE PRESENTATION SERVICES

6.1. Fetch Prescribed Items For Dispensing

Purpose:
Fetch items prescribed by doctor for pharmacy, lab, imaging, or other service delivery.

Method:
POST

Path:
/getPrescribeItemsList

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "trackingCode": "{prescription_tracking_code}",
  "type": "{prescription_type_code}",
  "orderType": "{order_type}"
}

orderType:
1 = prescription
2 = presentation / delivery

Output:
centerName
doctorName
trackingCode
hid
patientComplaintCode
patientComplaintDesc
nationalNumber
prescriptionDate
nomedicalSystem
medicalSystemType
detailList[]

6.2. Register Initial Electronic Dispensing

Purpose:
Register initial dispensing/service delivery for an electronic prescription.

Method:
POST

Path:
/registerInitialPrescription

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "trackingCode": "{prescription_tracking_code}",
  "nomedicalSystem": "{provider_medical_system_no}",
  "medicalSystemType": "1",
  "noTechnicalManager": "{technical_manager_no}",
  "noTechnicalManagerType": "1",
  "mobile": "{patient_mobile}",
  "serviceRequestModelList": []
}

Service item fields:
prescribeServiceUuid
terminology
type
code
count
prescribeCount
consumption
consumptionInstruction
description
bulkId
otc
emergency
orderType
deliveredDate

Output:
serviceResponseModelList[]
message
trackingCode
referringCode
prescriptionUuid

6.3. Confirm Final Dispensing

Purpose:
Finalize electronic or paper dispensing/service presentation after initial registration.

Method:
POST

Path:
/confirmPrescriptionPresentation

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "referringCode": "{referring_code}",
  "prescriptionUuid": "{prescription_uuid}"
}

Output:
presentationUuid
trackingCode
hid

6.4. Delete Dispensing Record

Purpose:
Delete a created dispensing/presentation record.

Method:
POST

Path:
/deletePrescription

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "trackingCode": "{presentation_tracking_code}"
}

Output:
result

6.5. Inquiry Drug Authenticity

Purpose:
Check barcode/authenticity for drug dispensing.

Method:
POST

Path:
/inquiryAuthenticity

Headers:
Common headers

Body:
{
  "code": "{service_or_drug_code}",
  "barcode": "{barcode}",
  "count": "{count}",
  "nationalNumber": "{patient_national_id}",
  "trackingCode": "{tracking_code}"
}

Output:
code
barcode
count
nationalNumber
message
outOrganizationCode

7. DISPENSING HISTORY SERVICES

7.1. Get Presentation History

Purpose:
Fetch dispensing/service delivery history in the current center.

Method:
POST

Path:
/getHistoryPresentation

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "fromDate": "{yyyyMMdd}",
  "toDate": "{yyyyMMdd}"
}

Output:
centerCode
memberCode
lastName
firstName
nationalNumber
prescribeTrackingCode
presentationTrackingCode
rowNumberInMonth
prescribeType
status
presentationDate
presentationUuid

7.2. Get Presentation History Details

Purpose:
Fetch item-level detail of a presentation history row.

Method:
POST

Path:
/getSubHistoryPresentation

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "presentationUuid": "{presentation_uuid}"
}

Output:
code
fullName
countPrescribe
countPresentation
countCommitment
consumption
consumptionInstruction
quotaOrganization
patientPayment
totalAmounts
bulkId
otc
emergency

8. PAPER PRESCRIPTION SERVICES

8.1. Register Initial Paper Prescription

Purpose:
Register initial paper prescription dispensing/service presentation.

Method:
POST

Path:
/registerInitialPaperPrescription

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "nomedicalSystem": "{provider_medical_system_no}",
  "medicalSystemType": "1",
  "noTechnicalManager": "{technical_manager_no}",
  "noTechnicalManagerType": "1",
  "mobile": "{patient_mobile}",
  "file": "{byte_array_or_base64}",
  "prescribeDate": "{yyyyMMdd}",
  "specialty": "{specialty_code}",
  "serviceRequestModelList": []
}

Output:
serviceResponseModelList[]
message
trackingCode
referringCode
prescriptionUuid

8.2. Confirm Paper Prescription

Purpose:
Finalize paper prescription dispensing after initial registration.

Method:
POST

Path:
/confirmPrescriptionPresentation

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "referringCode": "{referring_code}",
  "prescriptionUuid": "{prescription_uuid}"
}

Output:
presentationUuid
trackingCode
hid

9. DISPENSING EDIT SERVICES

9.1. Edit Dispensing

Purpose:
Edit a previously registered dispensing/service presentation.

Method:
POST

Path:
/editPrescription

Headers:
Common headers

Body:
{
  "nationalNumber": "{patient_national_id}",
  "prescriptionUuid": "{prescription_uuid}",
  "trackingCode": "{tracking_code}",
  "referringCode": "{referring_code}",
  "editServiceRequestModels": []
}

Edit item fields:
serviceUuid
code
count
prescribeCount
consumption
consumptionInstruction
description
bulkId
otc
emergency
type
orderType
deliveredDate

Output:
serviceResponseModelList[]
trackingCode
hid
referringCode
prescriptionUuid

10. TERMINOLOGY AND REFERENCE SERVICES

10.1. Convert Code By Terminology

Purpose:
Convert code between terminology systems, for example ERX to IRC or LOINC to LOINC.

Method:
POST

Path:
/convertByTerminology

Headers:
Common headers

Body:
{
  "code": "{source_code}",
  "source": "{source_terminology}",
  "target": "{target_terminology}"
}

Output:
code
title

11. COMMON PRESCRIPTION ITEM TYPES

Prescription type codes:
2 = Drug
3 = Laboratory
4 = Imaging / Radiology
5 = Hospitalization
6 = Rehabilitation / Physiotherapy
7 = Medical services and supplies
8 = Dentistry
9 = Medical equipment / glasses

Presentation type codes:
11 = Visit
12 = Drug
13 = Rehabilitation
14 = Imaging / Radiology
15 = Laboratory
16 = Medical services
17 = Dentistry
18 = Medical supplies

12. COMMON TERMINOLOGIES

ERX:
Drug or e-prescription coding.

IRC:
Iran drug/product code, especially for dispensing.

LOINC:
Laboratory and diagnostic service coding.

SNOMED CT:
Consumption timing and clinical terminology in some fields.

13. COMMON RESPONSE FORMAT

Most non-token DITAS calls return:

{
  "result": {
    "data": {},
    "status": {
      "statusCode": 200
    }
  },
  "status": {
    "statusCode": 200,
    "message": "OK"
  }
}

Application-level success may be inside:
result.data.status

Gateway-level success may be inside:
status.statusCode

14. COMMON ERROR CATEGORIES

200:
OK / successful call

202:
Accepted

400:
Bad request / invalid input

401:
Invalid credential

403:
Origin IP blocked or not allowed

404:
Not found

410:
Use HTTPS

429:
Too many requests or access denied

499:
Client closed request

500:
Internal server error

503:
Provider service error

504:
Backend timeout

520:
Unknown error

15. IMPLEMENTATION NOTES

1. Store both DITAS token and Armed Forces token separately.
2. Store user session separately from provider token.
3. Never call SAKHAD APIs directly from frontend code.
4. Keep all SAKHAD DTOs inside the ArmedForcesInsuranceAdapter.
5. Use provider_key = ARMED_FORCES for all persisted records.
6. Use request hash and idempotency key for submit, confirm, edit, and delete operations.
7. On timeout, fetch status/history before retrying.
8. Store remote IDs:
   - trackingCode
   - hid
   - referringCode
   - prescriptionUuid
   - presentationUuid
9. Support two-step flow for dispensing:
   - registerInitialPrescription
   - confirmPrescriptionPresentation
10. Support separate paper prescription flow.
11. Preserve provider-native status and map it to canonical Vitarish status.
12. Mask nationalNumber, barcode, token, sessionId, and payloads in logs.
13. Reference code sync must support ERX, IRC, LOINC, and provider-specific codes.
14. Exact endpoint availability must be verified against the official current API package.