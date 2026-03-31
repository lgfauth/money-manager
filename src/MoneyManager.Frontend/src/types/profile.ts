export interface UserProfileDto {
  name: string;
  fullName?: string;
  email: string;
  phone?: string;
  profilePicture?: string;
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
