export interface UserProfileDto {
  id?: string;
  name: string;
  fullName?: string;
  email: string;
  phone?: string;
  profilePicture?: string;
  preferredLanguage?: string;
  termsAcceptedAt?: string;
  termsVersion?: string;
  termsAccepted?: boolean;
}

export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface UpdateEmailDto {
  newEmail: string;
  password: string;
}

export interface AcceptTermsDto {
  termsVersion: string;
}
