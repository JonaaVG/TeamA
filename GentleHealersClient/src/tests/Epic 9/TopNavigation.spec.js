import { afterEach, describe, expect, it } from 'vitest'
import { mount, RouterLinkStub } from '@vue/test-utils'
import TopNavigation from '../../components/TopNavigation.vue'

const mountNavigation = (options = {}) =>
  mount(TopNavigation, {
    props: { profileName: 'Hendrik Jan', profileRole: 'Specialist' },
    global: { stubs: { RouterLink: RouterLinkStub } },
    ...options,
  })

describe('TopNavigation', () => {
  let wrapper

  afterEach(() => wrapper?.unmount())

  it('shows the signed-in user and role in the profile trigger', () => {
    wrapper = mountNavigation()

    expect(wrapper.get('.profile-name').text()).toBe('Hendrik Jan')
    expect(wrapper.get('.profile-role').text()).toBe('Specialist')
  })

  it('opens the navigation drawer and renders its menu slot', async () => {
    wrapper = mountNavigation({
      slots: { menu: '<a class="nav-menu-link" href="/roles">Rollenbeheer</a>' },
    })

    await wrapper.get('.menu-trigger').trigger('click')

    expect(wrapper.get('.menu-drawer').text()).toContain('Rollenbeheer')
    expect(wrapper.get('.menu-trigger').attributes('aria-expanded')).toBe('true')
  })

  it('closes open panels when Escape is pressed', async () => {
    wrapper = mountNavigation()
    await wrapper.get('.profile-trigger').trigger('click')
    expect(wrapper.find('#profile-panel').exists()).toBe(true)

    await wrapper.trigger('keydown', { key: 'Escape' })

    expect(wrapper.find('#profile-panel').exists()).toBe(false)
    expect(wrapper.get('.profile-trigger').attributes('aria-expanded')).toBe('false')
  })

  it('emits logout when the logout button is clicked', async () => {
    wrapper = mountNavigation()

    await wrapper.get('[aria-label="Uitloggen"]').trigger('click')

    expect(wrapper.emitted('logout')).toHaveLength(1)
  })
})
