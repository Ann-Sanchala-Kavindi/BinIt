import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { RegisterPage } from './RegisterPage';
import { authApi } from '../../../api/authApi';

vi.mock('../../../api/authApi', () => ({
  authApi: {
    register: vi.fn(),
  },
}));

describe('RegisterPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('verifies that no role input or selection field exists in the registration form', () => {
    render(
      <MemoryRouter>
        <RegisterPage />
      </MemoryRouter>
    );

    expect(screen.queryByLabelText(/role/i)).not.toBeInTheDocument();
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument();
    expect(screen.queryByRole('radio')).not.toBeInTheDocument();
  });

  it('rejects registration when password and confirm password do not match', async () => {
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <RegisterPage />
      </MemoryRouter>
    );

    await user.type(screen.getByLabelText(/full name/i), 'Kamal Silva');
    await user.type(screen.getByLabelText(/email address/i), 'kamal@example.com');
    await user.type(screen.getByLabelText(/phone number/i), '+94771234567');
    await user.type(screen.getByLabelText(/^password/i), 'Password123!');
    await user.type(screen.getByLabelText(/confirm password/i), 'DifferentPassword123!');

    await user.click(screen.getByRole('button', { name: /create citizen account/i }));

    expect(await screen.findByText(/passwords do not match/i)).toBeInTheDocument();
    expect(authApi.register).not.toHaveBeenCalled();
  });

  it('submits registration successfully without sending role data', async () => {
    const user = userEvent.setup();
    const mockResponse = {
      accessToken: 'token-123',
      expiresAt: '2026-09-07T16:00:00Z',
      user: {
        id: '22222222-2222-2222-2222-222222222222',
        fullName: 'Kamal Silva',
        email: 'kamal@example.com',
        role: 'Citizen',
      },
    };

    vi.mocked(authApi.register).mockResolvedValueOnce(mockResponse);

    render(
      <MemoryRouter>
        <RegisterPage />
      </MemoryRouter>
    );

    await user.type(screen.getByLabelText(/full name/i), 'Kamal Silva');
    await user.type(screen.getByLabelText(/email address/i), 'kamal@example.com');
    await user.type(screen.getByLabelText(/phone number/i), '+94771234567');
    await user.type(screen.getByLabelText(/^password/i), 'Password123!');
    await user.type(screen.getByLabelText(/confirm password/i), 'Password123!');

    await user.click(screen.getByRole('button', { name: /create citizen account/i }));

    await waitFor(() => {
      expect(authApi.register).toHaveBeenCalledWith({
        fullName: 'Kamal Silva',
        email: 'kamal@example.com',
        phoneNumber: '+94771234567',
        password: 'Password123!',
      });
    });

    expect(
      await screen.findByText(/registration successful! redirecting to login/i)
    ).toBeInTheDocument();
  });
});
