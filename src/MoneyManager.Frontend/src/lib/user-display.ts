export function getUserInitials(name?: string | null, email?: string | null): string {
  const normalizedName = name?.trim();

  if (normalizedName) {
    const parts = normalizedName
      .split(/\s+/)
      .filter(Boolean);

    if (parts.length >= 2) {
      return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
    }

    const firstPart = parts[0];
    return firstPart.slice(0, 2).toUpperCase();
  }

  const emailLocalPart = email?.split("@")[0]?.trim();
  if (emailLocalPart) {
    const sanitizedEmailPart = emailLocalPart.replace(/[^a-zA-Z0-9]/g, "");
    if (sanitizedEmailPart) {
      return sanitizedEmailPart.slice(0, 2).toUpperCase();
    }
  }

  return "MM";
}

export function getUserDisplayName(name?: string | null, email?: string | null): string {
  const normalizedName = name?.trim();
  if (normalizedName) {
    return normalizedName;
  }

  const normalizedEmail = email?.trim();
  if (normalizedEmail) {
    return normalizedEmail;
  }

  return "Usuario";
}

export function getProfileImageSrc(profilePicture?: string | null): string | undefined {
  const normalizedUrl = profilePicture?.trim();
  return normalizedUrl ? normalizedUrl : undefined;
}