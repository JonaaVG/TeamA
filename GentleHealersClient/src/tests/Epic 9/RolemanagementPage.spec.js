import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import api from '@/services/api'
import RolemanagementPage from '../../views/Systemadministrator/RolemanagementPage.vue'

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), put: vi.fn() },
}))

const roles = [
  { roleId: 1, name: 'Patiënt' },
  { roleId: 2, name: 'Specialist' },
]

const initialUsers = () => [
  {
    userId: 42,
    firstName: 'Hendrik',
    lastName: 'Jan',
    email: 'hendrik.jan@example.com',
    roleId: 1,
    roleName: 'Patiënt',
    status: 'Active',
  },
  {
    userId: 43,
    firstName: 'Alice',
    lastName: 'Bakker',
    email: 'alice@example.com',
    roleId: 2,
    roleName: 'Specialist',
    status: 'Inactive',
  },
]

const mountPage = async () => {
  const wrapper = mount(RolemanagementPage)
  await flushPromises()
  return wrapper
}

describe('RolemanagementPage', () => {
  let wrapper

  beforeEach(() => {
    api.get.mockImplementation((url) => {
      if (url.endsWith('/roles')) return Promise.resolve({ data: roles })
      if (url.endsWith('/users')) return Promise.resolve({ data: initialUsers() })
      throw new Error(`Unexpected GET ${url}`)
    })
    api.put.mockResolvedValue({ data: 'ok' })
  })

  afterEach(() => wrapper?.unmount())

  it('loads roles, users, and account status from the API', async () => {
    wrapper = await mountPage()

    expect(api.get).toHaveBeenCalledWith('/admin/roles/roles')
    expect(api.get).toHaveBeenCalledWith('/admin/roles/users')
    expect(wrapper.text()).toContain('Hendrik Jan')
    expect(wrapper.text()).toContain('Actief')
    expect(wrapper.text()).toContain('Inactief')
  })

  it('filters users by a case-insensitive name search', async () => {
    wrapper = await mountPage()
    await wrapper.get('#user-search').setValue('ALICE')

    expect(wrapper.text()).toContain('Alice Bakker')
    expect(wrapper.text()).not.toContain('Hendrik Jan')
  })

  it('sends the selected role and shows a success message', async () => {
    wrapper = await mountPage()
    const row = wrapper.findAll('.user-row').find((item) => item.text().includes('Hendrik Jan'))
    await row.get('select').setValue(2)
    await row.get('button[aria-label="Rol wijzigen voor Hendrik Jan"]').trigger('click')
    await flushPromises()

    expect(api.put).toHaveBeenCalledWith('/admin/roles/user/42/role', { roleId: 2 })
    expect(wrapper.get('[role="status"]').text()).toContain('Hendrik Jan is succesvol gewijzigd')
  })

  it('shows an error and leaves the role unchanged when role update fails', async () => {
    api.put.mockRejectedValueOnce({ response: { data: 'Wijzigen geweigerd.' } })
    wrapper = await mountPage()
    const row = wrapper.findAll('.user-row').find((item) => item.text().includes('Hendrik Jan'))
    await row.get('select').setValue(2)
    await row.get('button[aria-label="Rol wijzigen voor Hendrik Jan"]').trigger('click')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toBe('Wijzigen geweigerd.')
    expect(row.get('select').element.value).toBe('1')
    expect(api.put).toHaveBeenCalledWith('/admin/roles/user/42/role', { roleId: 2 })
  })

  it('deactivates an active account and updates its displayed status', async () => {
    wrapper = await mountPage()
    const row = wrapper.findAll('.user-row').find((item) => item.text().includes('Hendrik Jan'))
    await row.findAll('button').find((button) => button.text().includes('Account deactiveren')).trigger('click')
    await flushPromises()

    expect(api.put).toHaveBeenCalledWith('/admin/roles/user/42/deactivate')
    expect(row.text()).toContain('Inactief')
    expect(row.text()).toContain('Account activeren')
    expect(wrapper.get('[role="status"]').text()).toContain('is gedeactiveerd')
  })

  it('restores the action state and shows an error when deactivation fails', async () => {
    api.put.mockRejectedValueOnce({ response: { data: 'Deactiveren mislukt.' } })
    wrapper = await mountPage()
    const row = wrapper.findAll('.user-row').find((item) => item.text().includes('Hendrik Jan'))
    await row.findAll('button').find((button) => button.text().includes('Account deactiveren')).trigger('click')
    await flushPromises()

    expect(row.text()).toContain('Actief')
    expect(row.text()).toContain('Account deactiveren')
    expect(wrapper.get('[role="alert"]').text()).toBe('Deactiveren mislukt.')
  })

  it('shows a loading error when the roles or users request fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    api.get.mockRejectedValueOnce(new Error('network error'))
    wrapper = await mountPage()

    expect(wrapper.get('[role="alert"]').text()).toContain('konden niet worden geladen')
    consoleError.mockRestore()
  })
})
