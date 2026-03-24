import re
raw = open(r"e:\_Projetos\money-manager\src\MoneyManager.Web\wwwroot\i18n\pt-BR.json", encoding="utf-8").read()
for key in ["Settings", "Login", "Dashboard", "Transactions", "Accounts", "Categories"]:
    positions = [raw[:m.start()].count("\n")+1 for m in re.finditer('"' + key + r'"\s*:', raw)]
    print(key, "->", positions)
