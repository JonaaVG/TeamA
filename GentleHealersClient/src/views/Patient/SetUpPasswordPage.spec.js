import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import nl from '../../services/nl.json'
import SetUpPasswordPage from './SetUpPasswordPage.vue'

const mocks = vi.hoisted(() => ({
  route: { query: {} },
  push: vi.fn(),
  fetch: vi.fn(),
}))

vi.mock('vue-router', () => ({
  useRoute: () => mocks.route,
  useRouter: () => ({ push: mocks.push }),
}))

const translations = nl.SetUpPasswordPage
const email = 'user@example.com'
const token = 'activation-token'
const validPassword = 'StrongPassword1!'
const wrappers = []

function mountPage() {
  const wrapper = mount(SetUpPasswordPage, {
    global: {
      stubs: {
        RouterLink: { template: '<a><slot /></a>' },
      },
    },
  })
  wrappers.push(wrapper)
  return wrapper
}

async function submitPassword(wrapper, password = validPassword, confirmation = password) {
  await wrapper.get('#password').setValue(password)
  await wrapper.get('#confirmPassword').setValue(confirmation)
  await wrapper.find('form').trigger('submit')
  await flushPromises()
}

describe('SetUpPasswordPage', () => {
  beforeEach(() => {
    mocks.route.query = { email, token }
    mocks.push.mockReset()
    mocks.fetch.mockReset()
    mocks.fetch.mockResolvedValue({ ok: true })
    vi.stubGlobal('fetch', mocks.fetch)
  })

  afterEach(() => {
    wrappers.splice(0).forEach((wrapper) => wrapper.unmount())
    vi.unstubAllGlobals()
  })

  it('does not call the API when the activation link has no query values', async () => {
    mocks.route.query = {}
    const wrapper = mountPage()
    await flushPromises()

    expect(mocks.fetch).not.toHaveBeenCalled()
    expect(wrapper.get('[role="alert"]').text()).toContain(translations.invalidTitle)
  })

  it('validates the activation link and shows the password form when valid', async () => {
    const wrapper = mountPage()
    await flushPromises()

    expect(mocks.fetch).toHaveBeenCalledWith(
      '/api/users/validate-activation-token',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ token, email }),
      }),
    )
    expect(wrapper.get('#password').exists()).toBe(true)
    expect(wrapper.get('#confirmPassword').exists()).toBe(true)
  })

  it('shows the invalid-link message when token validation fails', async () => {
    mocks.fetch.mockResolvedValueOnce({ ok: false })
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain(translations.invalidTitle)
    expect(wrapper.find('#password').exists()).toBe(false)
  })

  it('blocks submission when the password confirmation does not match', async () => {
    const wrapper = mountPage()
    await flushPromises()

    await submitPassword(wrapper, validPassword, 'DifferentPassword2!')

    expect(wrapper.get('[role="alert"]').text()).toContain(translations.passwordMismatch)
    expect(mocks.fetch).toHaveBeenCalledTimes(1)
  })

  it.each([
    ['Ab1!short', translations.minimumLength],
    ['Abcdefghijkl!', translations.minimumDigit],
    ['abcdefghijkl1!', translations.minimumUppercase],
    ['ABCDEFGHIJKL1!', translations.minimumLowercase],
    ['Abcdefghijkl1', translations.minimumSymbol],
  ])('blocks a password that fails its policy: %s', async (password, message) => {
    const wrapper = mountPage()
    await flushPromises()

    await submitPassword(wrapper, password)

    expect(wrapper.get('[role="alert"]').text()).toContain(message)
    expect(mocks.fetch).toHaveBeenCalledTimes(1)
  })

  it('submits a valid password and displays the success message', async () => {
    const wrapper = mountPage()
    await flushPromises()

    await submitPassword(wrapper)

    expect(mocks.fetch).toHaveBeenNthCalledWith(
      2,
      '/api/users/set-password',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ email, token, password: validPassword }),
      }),
    )
    expect(wrapper.get('[role="status"]').text()).toContain(translations.passwordSetSuccess)
    expect(wrapper.find('#password').exists()).toBe(false)
  })

  it('shows an error when the API rejects the password update', async () => {
    mocks.fetch
      .mockResolvedValueOnce({ ok: true })
      .mockResolvedValueOnce({ ok: false })
    const wrapper = mountPage()
    await flushPromises()

    await submitPassword(wrapper)

    expect(wrapper.get('[role="alert"]').text()).toContain(translations.setPasswordFailed)
  })
})
