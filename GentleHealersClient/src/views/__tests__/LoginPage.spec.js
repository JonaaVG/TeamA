import { flushPromises, mount } from '@vue/test-utils'
import api from '@/services/api'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import LoginPage from '../LoginPage.vue'

const push = vi.fn()

vi.mock('@/services/api', () => ({
  default: {
    post: vi.fn(),
  },
}))

vi.mock('vue-router', () => ({
  useRouter: () => ({ push }),
}))

const mountLoginPage = () =>
  mount(LoginPage, {
    global: {
      stubs: {
        RouterLink: {
          props: ['to'],
          template: '<a :href="to"><slot /></a>',
        },
      },
    },
  })

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    const storage = new Map()
    vi.stubGlobal('localStorage', {
      getItem: key => storage.get(key) ?? null,
      setItem: (key, value) => storage.set(key, String(value)),
    })
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows a validation message when fields are empty', async () => {
    const wrapper = mountLoginPage()

    await wrapper.find('form').trigger('submit')

    expect(wrapper.get('[role="alert"]').text()).toContain(
      'Vul zowel je e-mailadres als wachtwoord in.',
    )
    expect(api.post).not.toHaveBeenCalled()
  })

  it('toggles password visibility', async () => {
    const wrapper = mountLoginPage()
    const password = wrapper.get('#password')
    const toggle = wrapper.get('button[type="button"]')

    expect(password.attributes('type')).toBe('password')
    await toggle.trigger('click')
    expect(password.attributes('type')).toBe('text')
  })

  it('logs in, stores the token and navigates to the dashboard', async () => {
    api.post.mockResolvedValue({
      data: { token: 'test-token' },
    })
    const wrapper = mountLoginPage()

    await wrapper.get('#email').setValue('jona@example.com')
    await wrapper.get('#password').setValue('secret123')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(api.post).toHaveBeenCalledWith('/auth/login', {
      email: 'jona@example.com',
      password: 'secret123',
    })
    expect(localStorage.getItem('token')).toBe('test-token')
    expect(push).toHaveBeenCalledWith('/')
  })

  it('shows an API error message from the response', async () => {
    api.post.mockRejectedValue({
      response: { data: { message: 'Account niet gevonden.' } },
    })
    const wrapper = mountLoginPage()

    await wrapper.get('#email').setValue('unknown@example.com')
    await wrapper.get('#password').setValue('wrong')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain('Account niet gevonden.')
  })

  it('shows a fallback message when the API is unreachable', async () => {
    api.post.mockRejectedValue(new Error('Network error'))
    const wrapper = mountLoginPage()

    await wrapper.get('#email').setValue('jona@example.com')
    await wrapper.get('#password').setValue('secret123')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain(
      'Inloggegevens onjuist of account is niet actief.',
    )
  })
})
