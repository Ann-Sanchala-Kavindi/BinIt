import { axiosClient } from './axiosClient';
import type {
  AuthResponse,
  CurrentUserResponse,
  LoginRequest,
  RegisterRequest,
} from '../features/auth/types';

export const authApi = {
  /**
   * Public registration endpoint. Always creates a Citizen role.
   */
  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await axiosClient.post<AuthResponse>('/auth/register', data);
    return response.data;
  },

  /**
   * User login endpoint with email and password.
   */
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await axiosClient.post<AuthResponse>('/auth/login', data);
    return response.data;
  },

  /**
   * Retrieves profile of currently authenticated user using JWT token.
   */
  getMe: async (): Promise<CurrentUserResponse> => {
    const response = await axiosClient.get<CurrentUserResponse>('/auth/me');
    return response.data;
  },
};
