namespace Vitarish.Clinic.Agents;

/// <summary>
/// The app-owned workflow instructions of the tool-free operations-help entry. Every step names a screen, button or
/// label that exists in the Darman clinic Web UI (English and Persian label side by side, taken from the clinic
/// localization), so the model explains real screens instead of inventing them. The entry has no tools: this text is
/// its only knowledge of the product.
/// </summary>
internal static class OperationsHelpGuide
{
    /// <summary>The questions the guide covers. Any other workflow question is answered as not covered.</summary>
    public static IReadOnlyList<string> SupportedQuestions { get; } =
    [
        "How do I book a visit for a patient?",
        "How do I see the schedule by day, week or month?",
        "How do I confirm a pending reservation?",
        "How do I delete a reservation?",
        "How do I create a shift (weekday, hours and seats)?",
        "How do I edit or delete a shift?"
    ];

    public static string Instructions { get; } = """
        Darman workflow guide. Each name is written as English / Persian. When you answer, use the names of the
        language the user wrote in, in the order of the steps, and keep the answer short.

        BOOK A VISIT FOR A PATIENT
        1. Open "Schedule" / «نوبت‌ها» in the sidebar.
        2. Press "Book a visit" / «ثبت نوبت». This switches the page from "List" / «فهرست» to "Calendar" / «تقویم»; you can also switch to "Calendar" yourself.
        3. Use "Day" / «روز», "Week" / «هفته» or "Month" / «ماه», "Today" / «امروز» and the previous and next buttons to reach the wanted day.
        4. Select the shift of that day ("Book this shift" / «رزرو این شیفت»). A shift marked "Full" / «تکمیل» has no free seat and cannot be booked.
        5. In the "Add Reservation" / «افزودن رزرو» dialog, which shows the date, the shift time and the seats left ("{available} of {total} seats available" / «{available} از {total} صندلی موجود», where the braces stand for numbers), find the patient or create a new patient.
        6. Press "Book" / «رزرو کردن». The screen shows "Reservation created!" / «رزرو ایجاد شد!» and the reservation is confirmed at once.

        SEE THE SCHEDULE BY DAY, WEEK OR MONTH
        Open "Schedule" / «نوبت‌ها». "List" / «فهرست» browses visits and "Calendar" / «تقویم» shows the shifts; in the calendar choose "Day" / «روز», "Week" / «هفته» or "Month" / «ماه».

        CONFIRM A PENDING RESERVATION
        Open "Schedule" / «نوبت‌ها», switch to "Calendar" / «تقویم», select the reservation to open "Reservation Details" / «جزئیات رزرو» and press "Confirm" / «تأیید». The button is shown only while the status is "Pending" / «در انتظار».

        DELETE A RESERVATION
        In the same "Reservation Details" / «جزئیات رزرو» dialog press "Delete" / «حذف» and accept "Delete this reservation?" / «این رزرو حذف شود؟».

        CREATE A SHIFT
        1. Open "Settings" / «تنظیمات» in the sidebar, then "Appointment Settings" / «تنظیمات نوبت‌دهی».
        2. Select the "Shifts" / «شیفت‌ها» tile and press "New Shift" / «شیفت جدید».
        3. Fill "Day of Week" / «روز هفته», "Start Time" / «زمان شروع», "End Time" / «زمان پایان» and "Total Seats" / «ظرفیت کل». The provider and the service are optional; "General" / «عمومی» means the shift is not linked to a service.
        4. Press "Save" / «ذخیره».

        EDIT OR DELETE A SHIFT
        1. Open "Commands" / «فرمان‌ها» in the toolbar and select "Shifts" / «شیفت‌ها».
        2. Select the shift in the list to open its details.
        3. To edit the shift, press "Edit" / «ویرایش», change the fields and press "Save" / «ذخیره».
        4. To delete the shift, press "Delete" / «حذف» and confirm "Delete this shift?" / «این شیفت حذف شود؟».

        NOT COVERED
        Anything else (for example check-in, rescheduling, cancelling, rejecting, marking a patient present or completing a visit, services, providers, reminders, surgeries or reports) is not described in this guide. Say that you are not sure how that screen works and do not describe steps you were not given. Never invent a screen, button, setting or feature.
        """;
}
