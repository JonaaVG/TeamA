import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import api from '@/services/api'
import PatientReferralPage from '@/views/Patient/PatientReferralPage.vue'

const { route, back } = vi.hoisted(() => ({
  route: { params: { patientId: '101' } },
  back: vi.fn(),
}))

vi.mock('vue-router', () => ({
  useRoute: () => route,
  useRouter: () => ({ back }),
}))

vi.mock('@/services/api', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}))

const treatments = [
  { treatmentCodeId: 1, careCode: 'Z0005', description: 'Knieonderzoek', specialtyId: 1, durationQuarters: 2 },
  { treatmentCodeId: 2, careCode: 'Z0006', description: 'Schouderonderzoek', specialtyId: 1, durationQuarters: 3 },
  { treatmentCodeId: 3, careCode: 'Z0007', description: 'Oogonderzoek', specialtyId: 2, durationQuarters: 2 },
]
const specialties = [
  { specialtyId: 1, name: 'Orthopedie' },
  { specialtyId: 2, name: 'Oogheelkunde' },
]

describe('Patient referral form in the GP portal', () => {
  let page

  async function openForm(patientId = '101') {
    page?.unmount()
    route.params.patientId = patientId
    page = mount(PatientReferralPage)
    await flushPromises()
  }

  beforeEach(async () => {
    // The existing frontend is mounted for real. Only its API calls use fixtures.
    api.get.mockImplementation(async (url) => {
      if (url === '/treatment-codes/active') return { data: treatments }
      if (url === '/specialties') return { data: specialties }
      throw new Error(`Unexpected GET ${url}`)
    })
    api.post.mockResolvedValue({ status: 201 })
    await openForm()
  })

  afterEach(() => {
    page?.unmount()
    page = undefined
  })

  async function addTreatment(treatmentCodeId, { duration, note } = {}) {
    await page.get('#treatment-code').setValue(String(treatmentCodeId))
    if (duration !== undefined) await page.get('#appointment-duration').setValue(String(duration))
    if (note !== undefined) await page.get('#specialist-notes').setValue(note)
    await page.get('form').trigger('submit')
  }

  function treatmentOptions() {
    return page.findAll('#treatment-code option').map(option => option.element.value)
  }

  async function submitForm() {
    const submit = page.findAll('button').find(button => button.text() === 'Aanmaken')
    await submit.trigger('click')
    await flushPromises()
  }

  function expectRequest(referrals, patientId = '101') {
    expect(api.post).toHaveBeenLastCalledWith(`/patient/${patientId}/referrals`, { referrals })
  }

  it('TC 4.2.1-01 shows one selected treatment and includes it in the request', async () => {
    await addTreatment(1)
    expect(page.findAll('tbody tr')).toHaveLength(1)
    expect(page.get('tbody tr').text()).toContain('Z0005')

    await submitForm()

    expectRequest([{ treatmentCodeId: 1, durationMinutes: 30, notes: null }])
    expect(back).toHaveBeenCalledOnce()
  })

  it('TC 4.2.1-02 sends distinct selected codes in one request', async () => {
    await addTreatment(1)
    await addTreatment(2)
    await addTreatment(3)

    expect(page.findAll('tbody tr')).toHaveLength(3)
    await submitForm()

    expect(api.post).toHaveBeenCalledOnce()
    expectRequest([
      { treatmentCodeId: 1, durationMinutes: 30, notes: null },
      { treatmentCodeId: 2, durationMinutes: 45, notes: null },
      { treatmentCodeId: 3, durationMinutes: 30, notes: null },
    ])
  })

  it('TC 4.2.1-03 prevents selecting the same code a second time', async () => {
    await addTreatment(1)

    expect(treatmentOptions()).not.toContain('1')
    await page.get('form').trigger('submit')
    expect(page.findAll('tbody tr')).toHaveLength(1)

    await submitForm()

    expectRequest([{ treatmentCodeId: 1, durationMinutes: 30, notes: null }])
  })

  it('TC 4.2.1-04 blocks submission without an available code and shows a message', async () => {
    expect(page.get('#treatment-code').text()).not.toContain('NIE03')
    await page.get('form').trigger('submit')
    expect(page.findAll('tbody tr')).toHaveLength(0)

    await submitForm()

    expect(page.get('[role="alert"]').text()).toBe('Voeg minimaal één behandelcode toe.')
    expect(api.post).not.toHaveBeenCalled()

    await addTreatment(1)
    await submitForm()

    expectRequest([{ treatmentCodeId: 1, durationMinutes: 30, notes: null }])
  })

  it('TC 4.2.2-01 starts with the catalogue duration and sends the adjusted duration', async () => {
    await page.get('#treatment-code').setValue('1')
    expect(page.get('#appointment-duration').element.value).toBe('30')
    await page.get('#appointment-duration').setValue('45')
    await page.get('form').trigger('submit')
    expect(page.get('tbody tr').text()).toContain('45 min')

    await submitForm()

    expectRequest([{ treatmentCodeId: 1, durationMinutes: 45, notes: null }])
  })

  it('TC 4.2.2-02 sends each treatment with its own duration', async () => {
    await addTreatment(1, { duration: 45 })
    await addTreatment(2, { duration: 60 })

    await submitForm()

    expectRequest([
      { treatmentCodeId: 1, durationMinutes: 45, notes: null },
      { treatmentCodeId: 2, durationMinutes: 60, notes: null },
    ])
  })

  it('TC 4.2.2-03 offers only valid quarter-hour durations when editing a treatment', async () => {
    await addTreatment(1, { duration: 45 })
    await page.get('tbody tr').trigger('click')
    const duration = page.get('#appointment-duration')
    const options = Array.from(duration.element.options, option => Number(option.value))

    expect(options).toContain(45)
    expect(options.every(minutes => Number.isInteger(minutes) && minutes > 0 && minutes % 15 === 0)).toBe(true)
    expect(duration.element.value).toBe('45')
    await page.get('form').trigger('submit')
    await submitForm()

    expectRequest([{ treatmentCodeId: 1, durationMinutes: 45, notes: null }])
  })

  it('TC 4.2.2-04 keeps the adjusted duration in its own referral form', async () => {
    await addTreatment(2, { duration: 60 })
    await submitForm()
    expectRequest([{ treatmentCodeId: 2, durationMinutes: 60, notes: null }])

    await openForm('102')
    await page.get('#treatment-code').setValue('2')
    expect(page.get('#appointment-duration').element.value).toBe('45')
    await page.get('form').trigger('submit')
    await submitForm()

    expectRequest([{ treatmentCodeId: 2, durationMinutes: 45, notes: null }], '102')
  })

  it('TC 4.3.1-01 displays, reopens and sends the GP explanation', async () => {
    const note = 'Pijn aan de rechterknie sinds drie weken.'
    await addTreatment(1, { note })
    expect(page.get('tbody td[title]').text()).toBe(note)
    await page.get('tbody tr').trigger('click')
    expect(page.get('#specialist-notes').element.value).toBe(note)

    await submitForm()

    expectRequest([{ treatmentCodeId: 1, durationMinutes: 30, notes: note }])
  })

  it('TC 4.3.1-02 sends null for an omitted explanation', async () => {
    await addTreatment(1)
    await submitForm()

    expectRequest([{ treatmentCodeId: 1, durationMinutes: 30, notes: null }])
  })

  it('TC 4.3.1-03 sends a 500-character explanation intact', async () => {
    const note = 'a'.repeat(499) + '!'
    await addTreatment(1, { note })
    expect(page.get('tbody td[title]').attributes('title')).toBe(note)

    await submitForm()

    expectRequest([{ treatmentCodeId: 1, durationMinutes: 30, notes: note }])
  })

  it('TC 4.3.1-04 preserves accents and line breaks and renders markup as text in the draft', async () => {
    const note = 'Patiënt noemt café en coöperatie.\n<strong>Voorbeeldtekst</strong>'
    await addTreatment(1, { note })
    const displayedNote = page.get('tbody td[title]')

    expect(displayedNote.element.textContent).toBe(note)
    expect(displayedNote.find('strong').exists()).toBe(false)
    await submitForm()

    expectRequest([{ treatmentCodeId: 1, durationMinutes: 30, notes: note }])
  })

  it('TC 4.3.1-05 sends null when the explanation contains only spaces', async () => {
    await addTreatment(1, { note: '     ' })
    await submitForm()

    expectRequest([{ treatmentCodeId: 1, durationMinutes: 30, notes: null }])
  })
})
