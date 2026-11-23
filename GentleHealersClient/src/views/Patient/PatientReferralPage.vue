<template>
  <div class="min-h-screen bg-blue-50/70 p-4 sm:p-6">
    <div class="mx-auto max-w-7xl">
      <!-- Paginatitel -->
      <h1 id="page-title" tabindex="-1" class="mb-5 text-2xl font-bold text-blue-950">
        {{ t.title }}
      </h1>

      <!-- Hoofdkaart -->
      <form
        @submit.prevent="handleSubmit"
        class="rounded-xl border border-slate-300 bg-white p-4 shadow-xs sm:p-5"
      >
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
          <!-- Linkerkolom: Behandelcodes -->
          <div class="flex min-h-72 flex-col rounded-lg border border-slate-300 bg-slate-50/40 p-3.5">
            <!-- Behandelcode toevoegen balk -->
            <div class="relative">
              <div
                class="flex items-center justify-between rounded-lg border border-slate-400 bg-white px-3.5 py-2.5 focus-within:border-blue-950 focus-within:ring-1 focus-within:ring-blue-950"
              >
                <select
                  v-model="selectedCodeToAdd"
                  :aria-label="t.addCodeLabel"
                  class="w-full appearance-none bg-transparent pr-6 text-sm text-slate-800 focus:outline-none"
                >
                  <option value="" disabled>{{ t.addCodePlaceholder }}</option>
                  <option
                    v-for="code in availableCodes"
                    :key="code.id"
                    :value="code.id"
                  >
                    {{ code.code }} – {{ code.label }}
                  </option>
                </select>

                <button
                  type="button"
                  @click="addTreatmentCode"
                  :disabled="!selectedCodeToAdd"
                  :aria-label="t.addCodeButtonLabel"
                  class="ml-2 inline-flex size-6 shrink-0 items-center justify-center rounded text-slate-800 transition hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  <Plus class="size-4 stroke-[2.2]" aria-hidden="true" />
                </button>
              </div>
            </div>

            <!-- Lijst met geselecteerde behandelcodes -->
            <div v-if="selectedCodes.length" class="mt-3 space-y-2">
              <div
                v-for="item in selectedCodes"
                :key="item.id"
                class="flex items-center justify-between rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm"
              >
                <div>
                  <span class="font-semibold text-blue-950">{{ item.code }}</span>
                  <span class="ml-2 text-slate-600">{{ item.label }}</span>
                </div>
                <button
                  type="button"
                  @click="removeTreatmentCode(item.id)"
                  :aria-label="formatText(t.removeCodeLabel, { code: item.code })"
                  class="rounded p-1 text-slate-500 transition hover:bg-red-50 hover:text-red-700"
                >
                  <X class="size-4" aria-hidden="true" />
                </button>
              </div>
            </div>
          </div>

          <!-- Rechterkolom: Duur afspraak + Toelichting -->
          <div class="flex flex-col gap-4">
            <!-- Duur afspraak -->
            <div class="rounded-lg border border-slate-300 bg-slate-50/40 p-3.5">
              <div
                class="flex items-center justify-between rounded-lg border border-slate-400 bg-white px-3.5 py-2.5 focus-within:border-blue-950 focus-within:ring-1 focus-within:ring-blue-950"
              >
                <label for="appointment-duration" class="text-sm text-slate-800">
                  {{ t.durationLabel }}
                </label>

                <div class="relative flex items-center gap-3">
                  <select
                    id="appointment-duration"
                    v-model="durationMinutes"
                    class="appearance-none bg-transparent pr-6 text-right text-sm font-medium text-slate-900 focus:outline-none"
                  >
                    <option
                      v-for="minutes in durationOptions"
                      :key="minutes"
                      :value="minutes"
                    >
                      {{ formatText(t.durationUnit, { minutes }) }}
                    </option>
                  </select>
                  <ChevronDown
                    class="pointer-events-none absolute right-0 size-4 text-slate-700"
                    aria-hidden="true"
                  />
                </div>
              </div>
            </div>

            <!-- Toelichting voor specialist -->
            <div class="flex flex-1 flex-col rounded-lg border border-slate-300 bg-slate-50/40 p-3.5">
              <label for="specialist-notes" class="mb-2 text-sm text-slate-800">
                {{ t.notesLabel }}
              </label>

              <textarea
                id="specialist-notes"
                v-model="notes"
                rows="6"
                class="min-h-36 w-full flex-1 resize-none rounded-lg border border-slate-400 bg-white p-3 text-sm text-slate-900 focus:border-blue-950 focus:outline-none focus:ring-1 focus:ring-blue-950"
              />
            </div>
          </div>
        </div>

        <!-- Foutmelding indien aanwezig -->
        <p v-if="errorMessage" role="alert" class="mt-4 text-sm font-medium text-red-700">
          {{ errorMessage }}
        </p>

        <!-- Onderste actieknoppen -->
        <div class="mt-5 flex items-center justify-between gap-4">
          <button
            type="button"
            @click="handleCancel"
            class="rounded-lg border border-slate-400 bg-white px-6 py-2.5 text-sm font-bold text-blue-950 transition hover:bg-slate-50"
          >
            {{ t.cancelButton }}
          </button>

          <button
            type="submit"
            :disabled="isSubmitting"
            class="rounded-lg bg-blue-950 px-6 py-2.5 text-sm font-bold text-white transition hover:bg-blue-900 disabled:opacity-50"
          >
            {{ isSubmitting ? t.submitButtonLoading : t.submitButton }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Plus, ChevronDown, X } from 'lucide-vue-next'
import api from '@/services/api'
import nl from '@/features/patients/nl.json'

const t = nl.referral

const formatText = (template, vars = {}) =>
  Object.entries(vars).reduce(
    (str, [key, value]) => str.replaceAll(`{${key}}`, String(value)),
    template
  )

const route = useRoute()
const router = useRouter()

const patientId = computed(() => route.params.patientId)

// Voorbeeld behandelcodes — vervang of vul aan via je API indien nodig
const treatmentCodeOptions = ref([
  { id: 1, code: 'BC-101', label: 'Intake consult' },
  { id: 2, code: 'BC-102', label: 'Uitgebreid onderzoek' },
  { id: 3, code: 'BC-204', label: 'Vervolgbehandeling' },
  { id: 4, code: 'BC-305', label: 'Advies & behandelplan' }
])

const durationOptions = [15, 30, 45, 60, 90]

const selectedCodeToAdd = ref('')
const selectedCodes = ref([])
const durationMinutes = ref(45)
const notes = ref('')
const errorMessage = ref('')
const isSubmitting = ref(false)

const availableCodes = computed(() => {
  const chosenIds = new Set(selectedCodes.value.map((c) => c.id))
  return treatmentCodeOptions.value.filter((c) => !chosenIds.has(c.id))
})

const addTreatmentCode = () => {
  if (!selectedCodeToAdd.value) return
  const match = treatmentCodeOptions.value.find((c) => c.id === selectedCodeToAdd.value)
  if (match) {
    selectedCodes.value.push(match)
  }
  selectedCodeToAdd.value = ''
}

const removeTreatmentCode = (id) => {
  selectedCodes.value = selectedCodes.value.filter((c) => c.id !== id)
}

const handleCancel = () => {
  router.back()
}

const handleSubmit = async () => {
  errorMessage.value = ''

  if (!selectedCodes.value.length) {
    errorMessage.value = t.errors.noTreatmentCodes
    return
  }

  isSubmitting.value = true
  try {
    await api.post(`/patient/${patientId.value}/referrals`, {
      treatmentCodeIds: selectedCodes.value.map((c) => c.id),
      durationMinutes: durationMinutes.value,
      notes: notes.value.trim() || null
    })

    router.back()
  } catch (error) {
    errorMessage.value = error.response?.data?.message || t.errors.generic
  } finally {
    isSubmitting.value = false
  }
}
</script>