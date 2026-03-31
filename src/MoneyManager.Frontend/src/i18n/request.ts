import { getRequestConfig } from "next-intl/server";
import { defaultLocale } from "./config";

export default getRequestConfig(async () => {
  const locale = defaultLocale;
  const messages = (await import(`../../public/locales/${locale}.json`)).default;

  return {
    locale,
    messages,
  };
});
