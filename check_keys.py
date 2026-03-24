import json

with open(r"e:\_Projetos\money-manager\src\MoneyManager.Web\wwwroot\i18n\pt-BR.json", encoding="utf-8") as f:
    data = json.load(f)

settings = data.get("Settings", {})
push_keys_in_razor = [
    "PushNotifications", "PushNotificationsHelp", "PushNotSupported",
    "PushActive", "PushDisable", "PushSendTest", "PushDenied", "PushDeniedHelp",
    "PushInactive", "PushEnable", "PushRecurringProcessed", "PushRecurringProcessedHelp",
    "PushDailyReminder", "PushDailyReminderHelp", "PushEnabledSuccess",
    "PushPermissionDenied", "PushEnableError", "PushDisabledSuccess",
    "PushDisableError", "PushTestSent", "PushTestError"
]

print("Key presence check:")
for k in push_keys_in_razor:
    found = k in settings
    val = settings.get(k, "NOT FOUND")
    print(f"  {'OK' if found else 'MISSING'} {k!r} = {val!r}")
