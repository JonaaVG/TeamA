<template>
  <div class="p-6">
    <h1 id="page-title" tabindex="-1" class="text-3xl font-bold text-blue-950">
      {{ t.title }}
    </h1>
    <p class="mt-2 text-blue-950">{{ t.subtitle }}</p>

    <div class="mt-8 overflow-x-auto rounded-2xl border-2 border-sky-500 bg-white">
      <table class="w-full text-left text-blue-950">
        <thead>
          <tr class="border-b border-blue-950">
            <th class="px-4 py-4 font-semibold">{{ t.createdAt }}</th>
            <th class="px-4 py-4 font-semibold">{{ t.department }}</th>
            <th class="px-4 py-4 font-semibold">{{ t.treatment }}</th>
            <th class="px-4 py-4 font-semibold">{{ t.referredBy }}</th>
            <th class="px-4 py-4 font-semibold">{{ t.expiresAt }}</th>
            <th class="px-4 py-4 font-semibold">{{ t.action }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="isLoading">
            <td colspan="6" class="px-4 py-6 text-center">{{ t.loading }}</td>
          </tr>
          <tr v-else-if="errorMessage">
            <td colspan="6" role="alert" class="px-4 py-6 text-center text-red-700">
              {{ errorMessage }}
            </td>
          </tr>
          <tr v-else-if="!referrals.length">
            <td colspan="6" class="px-4 py-6 text-center">{{ t.empty }}</td>
          </tr>
          <tr
            v-for="r in referrals"
            :key="r.referralTreatmentId"
            class="border-b border-blue-950/60 last:border-b-0"
          >
            <td class="px-4 py-3">{{ formatDate(r.createdAt) }}</td>
            <td class="px-4 py-3">{{ r.department }}</td>
            <td class="px-4 py-3">{{ r.treatmentDescription }}</td>
            <td class="px-4 py-3">{{ r.generalPractitionerName }}</td>
            <td class="px-4 py-3">{{ formatDate(r.expiresAt) }}</td>
            <td class="px-4 py-3">
              <button
                type="button"
                class="text-sky-700 hover:underline focus-visible:outline focus-visible:outline-2"
                @click="plan(r.referralTreatmentId)"
              >
                {{ t.plan }} ›
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/services/api'
import nl from '@/features/patients/nl.json'

const t = nl.referrals

const route = useRoute()
const router = useRouter()

const referrals = ref([])
const isLoading = ref(true)
const errorMessage = ref('')

const formatDate = (iso) =>
  new Date(iso).toLocaleDateString('nl-NL', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })

const plan = (referralTreatmentId) => {
  // TODO: replace with the real appointment-planning route
  router.push({ name: 'plan-appointment', params: { referralTreatmentId } })
}

onMounted(async () => {
  try {
    const { patientId } = route.params
    const response = await api.get(`/referral/patient/${patientId}/referrals`)
    referrals.value = response.data
  } catch (error) {
    errorMessage.value =
      error.response?.status === 403 ? t.forbidden : t.error
  } finally {
    isLoading.value = false
  }
})
</script>