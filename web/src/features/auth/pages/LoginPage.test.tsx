import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { LoginPage } from './LoginPage';
import { authApi } from '../../../api/authApi';
import { useAuthStore } from '../../../store/authStore';

// Mock authApi
vi.mock('../../../api/authApi', () => ({
  authApi: {
    login: vi.fn(),
  },
}));

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.getState().logout();
  });

  it('validates that email and password are required', async () => {
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const submitBtn = screen.getByRole('button', { name: /sign in/i });
    await user.click(submitBtn);

    expect(await screen.findByText(/email is required/i)).toBeInTheDocument();
    expect(await screen.findByText(/password is required/i)).toBeInTheDocument();
    expect(authApi.login).not.toHaveBeenCalled();
  });

  it('rejects invalid email formats', async () => {
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const emailInput = screen.getByLabelText(/email address/i);
    const passwordInput = screen.getByLabelText(/password/i);
    const submitBtn = screen.getByRole('button', { name: /sign in/i });

    await user.type(emailInput, 'invalid-email-format');
    await user.type(passwordInput, 'Password123!');
    await user.click(submitBtn);

    expect(await screen.findByText(/valid email address/i)).toBeInTheDocument();
    expect(authApi.login).not.toHaveBeenCalled();
  });

  it('calls authApi.login and updates store on successful submit', async () => {
    const user = userEvent.setup();
    const mockAuthResponse = {
      accessToken: 'fake.jwt.token',
      expiresAt: '2026-09-07T16:00:00Z',
      user: {
        id: '11111111-1111-1111-1111-111111111111',
        fullName: 'Kamal Silva',
        email: 'kamal@example.com',
        role: 'Citizen',
      },
    };

    vi.mocked(authApi.login).mockResolvedValueOnce(mockAuthResponse);

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const emailInput = screen.getByLabelText(/email address/i);
    const passwordInput = screen.getByLabelText(/password/i);
    const submitBtn = screen.getByRole('button', { name: /sign in/i });

    await user.type(emailInput, 'kamal@example.com');
    await user.type(passwordInput, 'Password123!');
    await user.click(submitBtn);

    await waitFor(() => {
      expect(authApi.login).toHaveBeenCalledWith({
        email: 'kamal@example.com',
        password: 'Password123!',
      });
    });

    const storeState = useAuthStore.getState();
    expect(storeState.accessToken).toBe('fake.jwt.token');
    expect(storeState.user?.fullName).toBe('Kamal Silva');
    expect(storeState.isAuthenticated).toBe(true);
  });
});
