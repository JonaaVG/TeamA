import { afterEach, describe, expect, it } from 'vitest'
import { mount, RouterLinkStub } from '@vue/test-utils'
import TopNavigation from '../../components/TopNavigation.vue'

// Mount the navigation with representative signed-in user details.
// RouterLink is stubbed because these tests check the component, not Vue Router navigation.
const mountNavigation = (options = {}) =>
  mount(TopNavigation, {
    props: { profileName: 'Hendrik Jan', profileRole: 'Specialist' },
    global: { stubs: { RouterLink: RouterLinkStub } },
    ...options,
  })

describe('TopNavigation', () => {
  let wrapper

  // Remove the mounted component after each test to keep tests isolated.
  afterEach(() => wrapper?.unmount())

  // The profile trigger should display the current user's name and role from its props.
  it('shows the signed-in user and role in the profile trigger', () => {
    wrapper = mountNavigation()

    expect(wrapper.get('.profile-name').text()).toBe('Hendrik Jan')
    expect(wrapper.get('.profile-role').text()).toBe('Specialist')
  })

  // A menu slot lets the page supply its own links; opening the trigger should reveal them.
  it('opens the navigation drawer and renders its menu slot', async () => {
    wrapper = mountNavigation({
      slots: { menu: '<a class="nav-menu-link" href="/roles">Rollenbeheer</a>' },
    })

    await wrapper.get('.menu-trigger').trigger('click')

    expect(wrapper.get('.menu-drawer').text()).toContain('Rollenbeheer')
    expect(wrapper.get('.menu-trigger').attributes('aria-expanded')).toBe('true')
  })

  // Escape should dismiss an open profile panel and reset the trigger's expanded state.
  it('closes open panels when Escape is pressed', async () => {
    wrapper = mountNavigation()
    await wrapper.get('.profile-trigger').trigger('click')
    expect(wrapper.find('#profile-panel').exists()).toBe(true)

    await wrapper.trigger('keydown', { key: 'Escape' })

    expect(wrapper.find('#profile-panel').exists()).toBe(false)
    expect(wrapper.get('.profile-trigger').attributes('aria-expanded')).toBe('false')
  })

  // Logout is emitted as an event so the parent component can perform the actual sign-out.
  it('emits logout when the logout button is clicked', async () => {
    wrapper = mountNavigation()

    await wrapper.get('[aria-label="Uitloggen"]').trigger('click')

    expect(wrapper.emitted('logout')).toHaveLength(1)
  })
})
