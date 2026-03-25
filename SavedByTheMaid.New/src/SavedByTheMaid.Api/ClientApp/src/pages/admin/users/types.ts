export interface UserDto {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  phoneNumber: string | null;
  isActive: boolean;
  emailConfirmed: boolean;
  roles: string[];
  createdAt: string;
}

export interface RoleDto {
  id: string;
  name: string;
}

export interface UserFormData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  isActive: boolean;
  roles: string[];
}

export function getRoleColor(role: string): string {
  switch (role) {
    case 'Admin':
      return 'bg-red-100 text-red-700';
    case 'Employee':
      return 'bg-blue-100 text-blue-700';
    case 'Customer':
      return 'bg-green-100 text-green-700';
    default:
      return 'bg-gray-100 text-gray-700';
  }
}
