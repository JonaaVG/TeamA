<template>
  <main class="mx-auto w-full max-w-4xl p-4 sm:p-6">
    <h1 id="page-title" tabindex="-1" class="mb-5 text-2xl font-bold text-blue-950">
      {{ t.title }}
    </h1>

    <div v-if="confirmation" role="status" class="mb-4 rounded-lg border border-green-300 bg-green-50 p-4 text-green-900">
      <p class="font-semibold">{{ t.booked }}</p>
      <p>{{ confirmation.description }}</p>
      <p>{{ formatStartTime(confirmation.slot.startTime) }}</p>
      <p>{{ confirmation.slot.locationName }} · {{ confirmation.slot.specialistName }}</p>
    </div>

    <section aria-labelledby="open-treatments-heading" class="rounded-xl border border-slate-300 bg-white p-4 sm:p-5">
      <h2 id="open-treatments-heading" class="mb-3 text-lg font-semibold text-blue-950">
        {{ t.openTreatments }}
      </h2>
      <p v-if="loadingTreatments" role="status" class="text-slate-700">{{ t.loadingTreatments }}</p>
      <div v-else-if="treatmentsError">
        <p role="alert" class="text-red-700">{{ treatmentsError }}</p>
        <button type="button" class="mt-2 rounded-lg border border-blue-950 px-4 py-2 font-semibold text-blue-950"
          @click="loadTreatments">
          {{ t.retry }}
        </button>
      </div>
      <p v-else-if="!treatments.length" class="text-slate-700">{{ t.noTreatments }}</p>

      <ul v-else class="space-y-3">
        <li v-for="treatment in treatments" :key="treatment.referralTreatmentId"
          class="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-slate-300 bg-blue-50 p-3">
          <div>
            <p class="font-semibold text-blue-950">{{ treatment.description }}</p>
            <p class="text-sm text-slate-700">
              <span>{{ treatment.careCode }}</span> · {{ treatment.durationMinutes }} {{ t.minutes }}
            </p>
          </div>
          <button type="button" :disabled="loadingSlots || booking"
            class="rounded-lg bg-blue-950 px-4 py-2 font-semibold text-white hover:bg-blue-900 disabled:opacity-50"
            @click="selectTreatment(treatment)">
            {{ t.book }}
          </button>
        </li>
      </ul>
    </section>

    <form v-if="activeTreatment" class="mt-4 rounded-xl border border-slate-300 bg-white p-4 sm:p-5"
      @submit.prevent="confirmAppointment">
      <h2 class="mb-3 text-lg font-semibold text-blue-950">{{ activeTreatment.description }}</h2>
      <p v-if="loadingSlots" role="status" class="mb-3 text-slate-700">{{ t.loadingSlots }}</p>
      <p v-if="bookingError" role="alert" class="mb-3 text-red-700">{{ bookingError }}</p>
      <p v-else-if="!loadingSlots && !slots.length" class="mb-3 text-slate-700">{{ t.noSlots }}</p>

      <fieldset v-if="slots.length" :disabled="booking" class="mb-4">
        <legend class="mb-2 font-semibold text-blue-950">{{ t.selectSlot }}</legend>
        <label for="appointment-date" class="block text-sm font-semibold text-slate-800">
          {{ t.appointmentDay }}
        </label>
        <select id="appointment-date" v-model="appointmentDay"
          class="mb-3 mt-1 rounded-lg border border-slate-400 bg-white px-3 py-2"
          @change="selectedSlot = null">
          <option v-for="day in availableDays" :key="day" :value="day">{{ formatDay(day) }}</option>
        </select>
        <div class="grid gap-2 sm:grid-cols-2">
          <label v-for="slot in slotsForDay" :key="`${slot.startTime}-${slot.specialistId}-${slot.locationId}`"
            class="flex items-center gap-3 rounded-lg border border-slate-300 px-3 py-2">
            <input v-model="selectedSlot" type="radio" name="appointment-slot" :value="slot">
            <span>
              <span class="block font-semibold text-blue-950">{{ formatStartTime(slot.startTime) }}</span>
              <span class="text-sm text-slate-700">{{ slot.locationName }} · {{ slot.specialistName }}</span>
            </span>
          </label>
        </div>
      </fieldset>

      <div class="flex flex-wrap gap-3">
        <button type="button" :disabled="loadingSlots || booking"
          class="rounded-lg border border-blue-950 px-4 py-2 font-semibold text-blue-950 disabled:opacity-50"
          @click="activeTreatment = null">
          {{ t.cancel }}
        </button>
        <button v-if="slots.length" type="submit" :disabled="!selectedSlot || booking"
          class="rounded-lg bg-blue-950 px-4 py-2 font-semibold text-white hover:bg-blue-900 disabled:opacity-50">
          {{ booking ? t.confirming : t.confirm }}
        </button>
      </div>
    </form>
  </main>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import api from '@/services/api'
import t from '@/features/referralBooking/nl.json'

const treatmentsPath = '/patient/referral-treatments'
const timeFormatter = new Intl.DateTimeFormat('nl-NL', {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: 'UTC',
})
const dayFormatter = new Intl.DateTimeFormat('nl-NL', { dateStyle: 'full', timeZone: 'UTC' })

const treatments = ref([])
const activeTreatment = ref(null)
const slots = ref([])
const appointmentDay = ref('')
const selectedSlot = ref(null)
const confirmation = ref(null)
const loadingTreatments = ref(true)
const loadingSlots = ref(false)
const booking = ref(false)
const treatmentsError = ref('')
const bookingError = ref('')

const availableDays = computed(() => [...new Set(slots.value.map(slot => slot.startTime.slice(0, 10)))])
const slotsForDay = computed(() => slots.value.filter(slot => slot.startTime.startsWith(appointmentDay.value)))

function formatDay(day) {
  return dayFormatter.format(new Date(`${day}T00:00:00Z`))
}

function formatStartTime(startTime) {
  // The API sends Dutch local wall-clock time. Keep these hours in every browser time zone.
  return timeFormatter.format(new Date(`${startTime.slice(0, 19)}Z`))
}

async function loadTreatments() {
  loadingTreatments.value = true
  treatmentsError.value = ''
  try {
    const { data } = await api.get(treatmentsPath)
    treatments.value = data
  } catch (error) {
    treatmentsError.value = error.response?.data?.message || t.loadTreatmentsError
  } finally {
    loadingTreatments.value = false
  }
}

async function selectTreatment(treatment) {
  activeTreatment.value = treatment
  selectedSlot.value = null
  slots.value = []
  appointmentDay.value = ''
  bookingError.value = ''
  loadingSlots.value = true
  try {
    const { data } = await api.get(`${treatmentsPath}/${treatment.referralTreatmentId}/slots`)
    slots.value = data
    appointmentDay.value = availableDays.value[0] ?? ''
  } catch (error) {
    bookingError.value = error.response?.data?.message || t.loadSlotsError
  } finally {
    loadingSlots.value = false
  }
}

async function confirmAppointment() {
  if (!selectedSlot.value || booking.value) return

  const treatment = activeTreatment.value
  const slot = selectedSlot.value
  const { specialistId, locationId, startTime } = slot
  booking.value = true
  bookingError.value = ''
  try {
    await api.post(`${treatmentsPath}/${treatment.referralTreatmentId}/appointment`, {
      specialistId, locationId, startTime,
    })
    treatments.value = treatments.value.filter(item => item.referralTreatmentId !== treatment.referralTreatmentId)
    confirmation.value = { description: treatment.description, slot }
    activeTreatment.value = null
  } catch (error) {
    if (error.response?.status === 409) await selectTreatment(treatment)
    bookingError.value = error.response?.data?.message || t.bookingError
    selectedSlot.value = null
  } finally {
    booking.value = false
  }
}

onMounted(loadTreatments)
</script>
