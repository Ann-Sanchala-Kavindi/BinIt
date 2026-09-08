import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate } from 'react-router-dom';
import axios from 'axios';
import { authApi } from '../../../api/authApi';
import { registerSchema, type RegisterFormData } from '../schemas/authSchemas';

export const RegisterPage: React.FC = () => {
  const [serverError, setServerError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      fullName: '',
      email: '',
      phoneNumber: '',
      password: '',
      confirmPassword: '',
    },
  });

  const onSubmit = async (data: RegisterFormData) => {
    setServerError(null);
    setSuccessMessage(null);
    setIsLoading(true);

    try {
      // Intentionally omitting confirmPassword and strictly sending required RegisterRequest
      // Public registration assigns the Citizen role on the server.
      await authApi.register({
        fullName: data.fullName,
        email: data.email,
        phoneNumber: data.phoneNumber,
        password: data.password,
      });

      setSuccessMessage('Registration successful! Redirecting to login...');
      setTimeout(() => {
        navigate('/login', {
          replace: true,
          state: { registeredEmail: data.email },
        });
      }, 1500);
    } catch (err: unknown) {
      if (axios.isAxiosError(err)) {
        const detail =
          err.response?.data?.detail ||
          err.response?.data?.title ||
          'Registration failed. Please check your details.';
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
      <h2 style={styles.formTitle}>Create Citizen Account</h2>
      <p style={styles.formSubtitle}>
        Join the smart waste collection and recycling network
      </p>

      {successMessage && (
        <div style={styles.successBanner} role="status">
          {successMessage}
        </div>
      )}

      {serverError && (
        <div style={styles.errorBanner} role="alert">
          {serverError}
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} noValidate style={styles.form}>
        <div style={styles.formGroup}>
          <label htmlFor="fullName" style={styles.label}>
            Full Name
          </label>
          <input
            id="fullName"
            type="text"
            autoComplete="name"
            {...register('fullName')}
            style={{
              ...styles.input,
              borderColor: errors.fullName ? '#ef4444' : '#cbd5e1',
            }}
            placeholder="Kamal Silva"
          />
          {errors.fullName && (
            <span style={styles.fieldError}>{errors.fullName.message}</span>
          )}
        </div>

        <div style={styles.formGroup}>
          <label htmlFor="email" style={styles.label}>
            Email Address
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
            placeholder="kamal@example.com"
          />
          {errors.email && (
            <span style={styles.fieldError}>{errors.email.message}</span>
          )}
        </div>

        <div style={styles.formGroup}>
          <label htmlFor="phoneNumber" style={styles.label}>
            Phone Number
          </label>
          <input
            id="phoneNumber"
            type="tel"
            autoComplete="tel"
            {...register('phoneNumber')}
            style={{
              ...styles.input,
              borderColor: errors.phoneNumber ? '#ef4444' : '#cbd5e1',
            }}
            placeholder="+94771234567"
          />
          {errors.phoneNumber && (
            <span style={styles.fieldError}>{errors.phoneNumber.message}</span>
          )}
        </div>

        <div style={styles.formGroup}>
          <label htmlFor="password" style={styles.label}>
            Password
          </label>
          <input
            id="password"
            type="password"
            autoComplete="new-password"
            {...register('password')}
            style={{
              ...styles.input,
              borderColor: errors.password ? '#ef4444' : '#cbd5e1',
            }}
            placeholder="At least 8 characters (1 uppercase, 1 digit)"
          />
          {errors.password && (
            <span style={styles.fieldError}>{errors.password.message}</span>
          )}
        </div>

        <div style={styles.formGroup}>
          <label htmlFor="confirmPassword" style={styles.label}>
            Confirm Password
          </label>
          <input
            id="confirmPassword"
            type="password"
            autoComplete="new-password"
            {...register('confirmPassword')}
            style={{
              ...styles.input,
              borderColor: errors.confirmPassword ? '#ef4444' : '#cbd5e1',
            }}
            placeholder="Re-enter your password"
          />
          {errors.confirmPassword && (
            <span style={styles.fieldError}>
              {errors.confirmPassword.message}
            </span>
          )}
        </div>

        <button
          type="submit"
          disabled={isLoading || Boolean(successMessage)}
          style={{
            ...styles.submitBtn,
            opacity: isLoading || successMessage ? 0.7 : 1,
            cursor: isLoading || successMessage ? 'not-allowed' : 'pointer',
          }}
        >
          {isLoading ? 'Creating account...' : 'Create Citizen Account'}
        </button>
      </form>

      <div style={styles.footer}>
        <span style={styles.footerText}>Already have an account? </span>
        <Link to="/login" style={styles.link}>
          Sign in
        </Link>
      </div>
    </div>
  );
};

const styles: Record<string, React.CSSProperties> = {
  formTitle: {
    margin: '0 0 0.25rem 0',
    fontSize: '1.25rem',
    fontWeight: 600,
    color: '#1e293b',
    textAlign: 'center',
  },
  formSubtitle: {
    margin: '0 0 1.25rem 0',
    fontSize: '0.875rem',
    color: '#64748b',
    textAlign: 'center',
  },
  successBanner: {
    backgroundColor: '#f0fdf4',
    color: '#15803d',
    padding: '0.75rem 1rem',
    borderRadius: '6px',
    fontSize: '0.875rem',
    marginBottom: '1rem',
    border: '1px solid #bbf7d0',
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
    gap: '0.875rem',
  },
  formGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.25rem',
  },
  label: {
    fontSize: '0.8125rem',
    fontWeight: 500,
    color: '#334155',
  },
  input: {
    padding: '0.5rem 0.75rem',
    borderRadius: '6px',
    border: '1px solid #cbd5e1',
    fontSize: '0.9rem',
    outline: 'none',
    boxSizing: 'border-box',
    width: '100%',
  },
  fieldError: {
    fontSize: '0.75rem',
    color: '#ef4444',
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
  },
  footer: {
    marginTop: '1.25rem',
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
