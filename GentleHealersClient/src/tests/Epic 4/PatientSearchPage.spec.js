import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import api from '@/services/api'
import { parseJwt } from '@/services/JwtParser.js'
import PatientSearchPage from '../../views/Patient/PatientSearchPage.vue'

vi.mock('@/services/api', () => ({
  default: { get: vi.fn() },
}))

vi.mock('@/services/JwtParser.js', () => ({
  parseJwt: vi.fn(),
}))

const patients = [
  {
    patientId: 17,
    firstName: 'Sanne',
    lastName: 'van der Berg',
    bsn: '111222333',
    dateOfBirth: '1985-06-14',
  },
]

const mountPage = async () => {
  const wrapper = mount(PatientSearchPage, {
    global: {
      stubs: {
        RouterLink: {
          props: ['to'],
          template: '<a :data-route="to.name" :data-patient-id="to.params.patientId"><slot /></a>',
        },
      },
    },
  })
  await flushPromises()
  return wrapper
}

describe('PatientSearchPage', () => {
  let wrapper

  beforeEach(() => {
    vi.stubGlobal('localStorage', { getItem: vi.fn(() => 'unit-test-token') })
    parseJwt.mockReturnValue({ role: 'GeneralPractitioner' })
    api.get.mockResolvedValue({ data: patients })
  })

  afterEach(() => {
    wrapper?.unmount()
    vi.unstubAllGlobals()
  })

  it('loads patients on page open and displays their identifying details', async () => {
    wrapper = await mountPage()

    expect(api.get).toHaveBeenCalledWith('/patient/search', { params: {} })
    expect(wrapper.text()).toContain('Sanne van der Berg')
    expect(wrapper.text()).toContain('111222333')
    expect(wrapper.text()).toContain('1985-06-14')
  })

  it('trims the search text and sends it to the patient search endpoint', async () => {
    wrapper = await mountPage()

    await wrapper.get('input[aria-label="Zoek patiënt op naam of BSN"]').setValue('  Sanne Berg  ')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(api.get).toHaveBeenLastCalledWith('/patient/search', {
      params: { query: 'Sanne Berg' },
    })
  })

  it('links a general practitioner to the referral page for the selected patient', async () => {
    wrapper = await mountPage()

    const referralLink = wrapper.get('a[data-route="patient-referral"]')
    expect(referralLink.text()).toContain('Doorverwijzen')
    expect(referralLink.attributes('data-patient-id')).toBe('17')
  })

  it('shows an empty-results message when no patients match', async () => {
    api.get.mockResolvedValueOnce({ data: [] })
    wrapper = await mountPage()

    expect(wrapper.text()).toContain('Geen patiënten gevonden.')
    expect(wrapper.findAll('ul li')).toHaveLength(0)
  })

  it('shows the API error message when the search request fails', async () => {
    api.get.mockRejectedValueOnce({ response: { data: { message: 'Zoeken is mislukt.' } } })
    wrapper = await mountPage()

    expect(wrapper.get('[role="alert"]').text()).toBe('Zoeken is mislukt.')
    expect(wrapper.findAll('ul li')).toHaveLength(0)
  })

  it('does not show practitioner actions when no recognized role is present', async () => {
    parseJwt.mockReturnValueOnce({ role: 'Patient' })
    wrapper = await mountPage()

    expect(wrapper.findAll('a[data-route="patient-referral"]')).toHaveLength(0)
  })
})
