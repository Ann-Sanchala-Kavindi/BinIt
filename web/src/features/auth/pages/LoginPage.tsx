import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate } from 'react-router-dom';
import axios from 'axios';
import { authApi } from '../../../api/authApi';
import { useAuthStore } from '../../../store/authStore';
import { loginSchema, type LoginFormData } from '../schemas/authSchemas';

export const LoginPage: React.FC = () => {
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();
  const { setAuth } = useAuthStore();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  });

  const onSubmit = async (data: LoginFormData) => {
    setServerError(null);
    setIsLoading(true);

    try {
      const response = await authApi.login(data);
      setAuth(response.accessToken, response.user);

      // Role-based destination: all authenticated roles route to / for now
      navigate('/', { replace: true });
    } catch (err: unknown) {
      if (axios.isAxiosError(err)) {
        const detail =
          err.response?.data?.detail ||
          err.response?.data?.title ||
          'Failed to sign in. Please check your credentials.';
        setServerError(detail);
      } else {
        setServerError('An unexpected error occurred. Please try again.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div>
      <h2 style={styles.formTitle}>Sign in to your account</h2>

      {serverError && (
        <div style={styles.errorBanner} role="alert">
          {serverError}
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} noValidate style={styles.form}>
        <div style={styles.formGroup}>
          <label htmlFor="email" style={styles.label}>
            Email address
          </label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            {...register('email')}
            style={{
              ...styles.input,
              borderColor: errors.email ? '#ef4444' : '#cbd5e1',
            }}
            placeholder="you@example.com"
          />
          {errors.email && (
            <span style={styles.fieldError}>{errors.email.message}</span>
          )}
        </div>

        <div style={styles.formGroup}>
          <label htmlFor="password" style={styles.label}>
            Password
          </label>
          <input
            id="password"
            type="password"
            autoComplete="current-password"
            {...register('password')}
            style={{
              ...styles.input,
              borderColor: errors.password ? '#ef4444' : '#cbd5e1',
            }}
            placeholder="••••••••"
          />
          {errors.password && (
            <span style={styles.fieldError}>{errors.password.message}</span>
          )}
        </div>

        <button
          type="submit"
          disabled={isLoading}
          style={{
            ...styles.submitBtn,
            opacity: isLoading ? 0.7 : 1,
            cursor: isLoading ? 'not-allowed' : 'pointer',
          }}
        >
          {isLoading ? 'Signing in...' : 'Sign in'}
        </button>
      </form>

      <div style={styles.footer}>
        <span style={styles.footerText}>Don't have an account? </span>
        <Link to="/register" style={styles.link}>
          Register as Citizen
        </Link>
      </div>
    </div>
  );
};

const styles: Record<string, React.CSSProperties> = {
  formTitle: {
    margin: '0 0 1.25rem 0',
    fontSize: '1.25rem',
    fontWeight: 600,
    color: '#1e293b',
    textAlign: 'center',
  },
  errorBanner: {
    backgroundColor: '#fef2f2',
    color: '#b91c1c',
    padding: '0.75rem 1rem',
    borderRadius: '6px',
    fontSize: '0.875rem',
    marginBottom: '1rem',
    border: '1px solid #fecaca',
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1rem',
  },
  formGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.375rem',
  },
  label: {
    fontSize: '0.875rem',
    fontWeight: 500,
    color: '#334155',
  },
  input: {
    padding: '0.625rem 0.875rem',
    borderRadius: '6px',
    border: '1px solid #cbd5e1',
    fontSize: '0.95rem',
    outline: 'none',
    boxSizing: 'border-box',
    width: '100%',
  },
  fieldError: {
    fontSize: '0.75rem',
    color: '#ef4444',
    marginTop: '0.125rem',
  },
  submitBtn: {
    marginTop: '0.5rem',
    padding: '0.75rem',
    backgroundColor: '#16a34a',
    color: '#ffffff',
    border: 'none',
    borderRadius: '6px',
    fontSize: '0.95rem',
    fontWeight: 600,
    transition: 'background-color 0.2s',
  },
  footer: {
    marginTop: '1.5rem',
    textAlign: 'center',
    fontSize: '0.875rem',
  },
  footerText: {
    color: '#64748b',
  },
  link: {
    color: '#16a34a',
    textDecoration: 'none',
    fontWeight: 600,
  },
};
