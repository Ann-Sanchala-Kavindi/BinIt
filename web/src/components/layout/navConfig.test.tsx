import { describe, expect, it } from 'vitest';
import { defaultOfficerNavItems, managerNavItems } from './navConfig';

describe('defaultOfficerNavItems', () => {
  it('includes the protected Dispatch & Routes destination alongside collection tasks', () => {
    expect(defaultOfficerNavItems).toEqual(expect.arrayContaining([
      expect.objectContaining({ label: 'Dispatch & Routes', to: '/officer/dispatch' }),
      expect.objectContaining({ label: 'Collection Tasks', to: '/officer/tasks' }),
      expect.objectContaining({ label: 'Complaints', to: '/officer/complaints' }),
    ]));
  });
});

describe('managerNavItems', () => {
  it('includes Complaints destination for MunicipalManager', () => {
    expect(managerNavItems).toEqual(expect.arrayContaining([
      expect.objectContaining({ label: 'Complaints', to: '/manager/complaints' }),
    ]));
  });
});

