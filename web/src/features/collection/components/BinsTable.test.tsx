import { beforeEach, describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { BinsTable } from './BinsTable';
import { useAuthStore } from '../../../store/authStore';

describe('BinsTable navigation', () => {
  beforeEach(() => useAuthStore.setState({ user: { id: 'officer', fullName: 'Officer', email: 'officer@example.com', role: 'WasteOfficer' }, isAuthenticated: true, isLoading: false, accessToken: 'test' }));
  it('links each bin code to its officer detail route', () => {
    render(<MemoryRouter><BinsTable isLoading={false} isFiltered={false} onClearFilters={() => {}} bins={[{ id: 'bin-1', binCode: 'BIN-COL-0042', latitude: 6.9271, longitude: 79.8612, addressText: null, capacityLiters: 660, administrativeStatus: 'Active', acceptedWasteTypes: ['General'], collectionWeekdays: [], latestFillLevelPercent: null, latestCondition: null, latestObservationAt: null, hasActiveTask: false, lastCollectedAt: null, createdAt: '2026-09-01T00:00:00Z' }]} /></MemoryRouter>);
    expect(screen.getByTestId('bin-details-bin-1')).toHaveAttribute('href', '/officer/bins/bin-1');
    expect(screen.getByTestId('manage-bin-bin-1')).toHaveTextContent('Manage Bin');
  });
  it('uses the read-only label for MunicipalManager', () => { useAuthStore.setState({ user: { id: 'manager', fullName: 'Manager', email: 'manager@example.com', role: 'MunicipalManager' }, isAuthenticated: true, isLoading: false, accessToken: 'test' }); render(<MemoryRouter><BinsTable isLoading={false} isFiltered={false} onClearFilters={() => {}} bins={[{ id: 'bin-1', binCode: 'BIN-COL-0042', latitude: 6.9271, longitude: 79.8612, addressText: null, capacityLiters: 660, administrativeStatus: 'Active', acceptedWasteTypes: ['General'], collectionWeekdays: [], latestFillLevelPercent: null, latestCondition: null, latestObservationAt: null, hasActiveTask: false, lastCollectedAt: null, createdAt: '2026-09-01T00:00:00Z' }]} /></MemoryRouter>); expect(screen.getByTestId('manage-bin-bin-1')).toHaveTextContent('View Details'); expect(screen.getByTestId('manage-bin-bin-1')).toHaveAttribute('href', '/officer/bins/bin-1'); });
});
