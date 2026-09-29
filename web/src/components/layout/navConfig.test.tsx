import { describe, expect, it } from 'vitest';
import { defaultOfficerNavItems } from './navConfig';

describe('defaultOfficerNavItems', () => {
  it('includes the protected Dispatch & Routes destination alongside collection tasks', () => {
    expect(defaultOfficerNavItems).toEqual(expect.arrayContaining([
      expect.objectContaining({ label: 'Dispatch & Routes', to: '/officer/dispatch' }),
      expect.objectContaining({ label: 'Collection Tasks', to: '/officer/tasks' }),
    ]));
  });
});
