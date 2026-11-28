<template>
  <main class="p-6">
    <h1 id="page-title" tabindex="-1" class="mb-6 text-2xl font-bold text-blue-950">
      {{ t.title }}
    </h1>

    <p v-if="loading" role="status">{{ t.loading }}</p>
    <p v-else-if="loadFailed" role="alert" class="text-red-700">{{ t.loadFailed }}</p>
    <p v-else-if="!appointments.length">{{ t.empty }}</p>
    <ul v-else class="space-y-3">
      <li
        v-for="appointment in appointments"
        :key="appointment.appointmentId"
        class="rounded-lg border border-slate-300 bg-white"
      >
        <button
          type="button"
          class="w-full rounded-lg px-4 py-3 text-left font-bold text-blue-950 hover:bg-blue-50 focus-visible:outline-2"
          :aria-expanded="expandedAppointmentId === appointment.appointmentId"
          :aria-controls="`appointment-${appointment.appointmentId}`"
          @click="toggleAppointment(appointment.appointmentId)"
        >
          {{ appointment.patientName }}
        </button>
        <dl
          v-show="expandedAppointmentId === appointment.appointmentId"
          :id="`appointment-${appointment.appointmentId}`"
          class="space-y-1 px-4 pb-4 text-slate-800"
        >
          <dt class="font-semibold">{{ t.startTime }}</dt>
          <dd><time :datetime="appointment.startTime">{{ formatStartTime(appointment.startTime) }}</time></dd>
          <template v-if="appointment.note?.trim()">
            <dt class="pt-2 font-semibold">{{ t.note }}</dt>
            <dd class="whitespace-pre-wrap break-words">{{ appointment.note }}</dd>
          </template>
        </dl>
      </li>
    </ul>
  </main>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import api from '@/services/api'
import t from '@/features/specialistAppointments/nl.json'

const appointments = ref([])
const expandedAppointmentId = ref(null)
const loading = ref(true)
const loadFailed = ref(false)
const formatter = new Intl.DateTimeFormat('nl-NL', { dateStyle: 'medium', timeStyle: 'short' })

function formatStartTime(startTime) {
  return formatter.format(new Date(startTime))
}

function toggleAppointment(appointmentId) {
  expandedAppointmentId.value = expandedAppointmentId.value === appointmentId ? null : appointmentId
}

onMounted(async () => {
  try {
    const response = await api.get('/appointments/specialist')
    appointments.value = response.data
  } catch {
    loadFailed.value = true
  } finally {
    loading.value = false
  }
})
</script>
