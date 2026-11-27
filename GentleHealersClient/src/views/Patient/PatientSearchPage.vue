<template>
  <div class="p-6">
    <h1 id="page-title" tabindex="-1" class="mb-6 text-2xl font-bold text-blue-950">
      {{ t.title }}
    </h1>

    <form class="mb-6 flex gap-4" @submit.prevent="handleSearch">
      <div class="relative flex-1">
        <Search class="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-500" aria-hidden="true" />
        <input
          v-model="query"
          type="text"
          :placeholder="t.placeholder"
          :aria-label="t.searchLabel"
          class="w-full rounded-lg border border-slate-300 bg-white py-2 pl-10 pr-3"
        />
      </div>
      <button
        type="submit"
        :disabled="isLoading"
        class="rounded-lg bg-blue-950 px-6 py-2 font-bold text-white disabled:opacity-50"
      >
        {{ isLoading ? t.buttonLoading : t.button }}
      </button>
    </form>

    <p v-if="errorMessage" role="alert" class="mb-4 text-red-700">{{ errorMessage }}</p>

    <div class="min-h-64 rounded-lg border border-slate-300 bg-white p-4">
      <!-- Live region: uses only translated strings from nl.json -->
      <div role="status" aria-live="polite" aria-atomic="true">
        <p v-if="hasSearched && !patients.length && !errorMessage" class="text-slate-500">
          {{ t.noResults }}
        </p>
        <p v-else-if="hasSearched && patients.length && !errorMessage" class="sr-only">
          {{ resultsAnnouncement }}
        </p>
      </div>

      <ul class="space-y-3">
        <li
          v-for="p in patients"
          :key="p.patientId"
          class="flex flex-col gap-3 rounded-lg border border-slate-300 bg-blue-50 p-3 sm:flex-row sm:items-center sm:justify-between"
        >
          <div>
            <h2 class="font-bold text-blue-950">{{ p.firstName }} {{ p.lastName }}</h2>
            <p class="text-sm text-slate-600">
              {{ t.bsn }}
              <span aria-hidden="true">{{ p.bsn }}</span>
              <span class="sr-only">{{ formatBsnForSpeech(p.bsn) }}</span>
            </p>
            <p class="text-sm text-slate-600">{{ t.dateOfBirth }} {{ p.dateOfBirth }}</p>
          </div>

          <div class="flex flex-wrap gap-2">
            <RouterLink
              v-for="a in actions"
              :key="a.key"
              :to="{ name: a.route, params: { patientId: p.patientId } }"
              :aria-label="getActionAriaLabel(a.key, p)"
              class="inline-flex items-center gap-2 rounded-lg bg-blue-950 px-3 py-2 text-sm font-bold text-white hover:bg-blue-900"
            >
              <component :is="a.icon" class="size-4" aria-hidden="true" />
              <span aria-hidden="true">{{ tActions[a.key] }}</span>
            </RouterLink>
          </div>
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { Search } from 'lucide-vue-next'
import api from '@/services/api'
import { parseJwt } from '@/services/JwtParser.js'
import { patientActionsByRole } from '@/features/patients/patientActions.js'
import nl from '@/features/patients/nl.json'

const t = nl.search
const tActions = nl.actions

const query = ref('')
const patients = ref([])
const isLoading = ref(false)
const errorMessage = ref('')
const hasSearched = ref(false)

const claims = computed(() => {
  const token = localStorage.getItem('token')
  return token ? parseJwt(token) : null
})
const userRole = computed(() => claims.value?.role ?? '')
const actions = computed(() => patientActionsByRole[userRole.value] ?? [])

// Format dynamic count announcement using nl.json templates
const resultsAnnouncement = computed(() => {
  if (patients.value.length === 1) {
    return t.resultsCountOne
  }
  return t.resultsCountMany.replace('{count}', String(patients.value.length))
})

// Space out digits so screen readers read BSN digit-by-digit instead of as a large number
const formatBsnForSpeech = (bsn) => {
  return String(bsn ?? '').replace(/\s+/g, '').split('').join(' ')
}

// Build accessible button label from nl.json template
const getActionAriaLabel = (actionKey, patient) => {
  const fullName = `${patient.firstName} ${patient.lastName}`.trim()
  return t.actionAriaLabel
    .replace('{action}', tActions[actionKey])
    .replace('{name}', fullName)
    .replace('{bsn}', formatBsnForSpeech(patient.bsn))
}

const fetchPatients = async (searchQuery = '') => {
  errorMessage.value = ''
  isLoading.value = true

  try {
    const params = searchQuery ? { query: searchQuery } : {}
    const response = await api.get('/patient/search', { params })
    patients.value = response.data
    hasSearched.value = true
  } catch (error) {
    patients.value = []
    errorMessage.value = error.response?.data?.message || t.errors.generic
  } finally {
    isLoading.value = false
  }
}

const handleSearch = async () => {
  const q = query.value.trim()
  await fetchPatients(q)
}

onMounted(() => {
  fetchPatients('')
})
</script>