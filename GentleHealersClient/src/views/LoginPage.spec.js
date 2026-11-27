import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import LoginPage from './LoginPage.vue'

const mocks = vi.hoisted(() => ({
  post: vi.fn(),
  push: vi.fn(),
}))

vi.mock('@/services/api', () => ({
  default: { post: mocks.post },
}))

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: mocks.push }),
}))

function mountLoginPage() {
  return mount(LoginPage, {
    global: {
      stubs: {
        RouterLink: { template: '<a><slot /></a>' },
      },
    },
  })
}

describe('LoginPage', () => {
  beforeEach(() => {
    mocks.post.mockReset()
    mocks.push.mockReset()
    localStorage.clear()
  })

  it('blocks submission when email or password is missing', async () => {
    const wrapper = mountLoginPage()

    await wrapper.find('form').trigger('submit')

    expect(wrapper.get('[role="alert"]').text()).toContain(
      'Vul zowel je e-mailadres als wachtwoord in.',
    )
    expect(mocks.post).not.toHaveBeenCalled()
  })

  it('sends credentials, stores the token, and redirects after a successful login', async () => {
    mocks.post.mockResolvedValue({ data: { token: 'jwt-token' } })
    const wrapper = mountLoginPage()

    await wrapper.get('#email').setValue('  user@example.com  ')
    await wrapper.get('#password').setValue('correct-password')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(mocks.post).toHaveBeenCalledWith('/auth/login', {
      email: 'user@example.com',
      password: 'correct-password',
    })
    expect(localStorage.getItem('token')).toBe('jwt-token')
    expect(mocks.push).toHaveBeenCalledWith('/')
  })

  it('shows the API error and re-enables the button when login fails', async () => {
    mocks.post.mockRejectedValue({
      response: { data: { message: 'Inloggegevens onjuist.' } },
    })
    const wrapper = mountLoginPage()

    await wrapper.get('#email').setValue('user@example.com')
    await wrapper.get('#password').setValue('wrong-password')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain('Inloggegevens onjuist.')
    expect(wrapper.get('button[type="submit"]').element.disabled).toBe(false)
    expect(mocks.push).not.toHaveBeenCalled()
  })
})
