import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { AppRoutes } from './AppRoutes';
import { useAuthStore } from '../store/authStore';

describe('AppRoutes - Registration Route Handling', () => {
  beforeEach(() => {
    useAuthStore.getState().logout();
    useAuthStore.setState({ isAuthenticated: false, isLoading: false, user: null, accessToken: null });
  });

  it('redirects /register to /login and does not render any registration form', () => {
    render(
      <MemoryRouter initialEntries={['/register']}>
        <AppRoutes />
      </MemoryRouter>
    );

    // Should be redirected to /login
    expect(screen.getByRole('heading', { name: /sign in to your account/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();

    // Confirm no registration form fields exist
    expect(screen.queryByRole('heading', { name: /create citizen account/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /create citizen account/i })).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/confirm password/i)).not.toBeInTheDocument();
  });
});
