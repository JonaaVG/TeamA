<template>
    <div class="min-h-screen bg-blue-50/70 p-4 sm:p-6">
        <div class="mx-auto max-w-7xl">
            <h1 id="page-title" tabindex="-1" class="mb-5 text-2xl font-bold text-blue-950">
                {{ t.title }}
            </h1>

            <div class="rounded-xl border border-slate-300 bg-white p-4 shadow-xs sm:p-5">
                <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">

                    <!-- Linkerkant: invoerformulier -->
                    <form @submit.prevent="saveEntry"
                        class="flex flex-col gap-3 rounded-lg border border-slate-300 bg-slate-50/40 p-3.5">
                        <h2 class="text-sm font-semibold text-blue-950">
                            {{ editingId ? t.editHeading : t.addHeading }}
                        </h2>

                        <!-- Specialisme (filter) -->
                        <div>
                            <label for="specialty" class="mb-1 block text-sm text-slate-800">
                                {{ t.specialtyFilterLabel }}
                            </label>
                            <select id="specialty" v-model="selectedSpecialtyId"
                                class="w-full rounded-lg border border-slate-400 bg-white px-3 py-2.5 text-sm text-slate-800 focus:border-blue-950 focus:outline-none focus:ring-1 focus:ring-blue-950">
                                <option value="">{{ t.allSpecialties }}</option>
                                <option v-for="s in specialties" :key="s.specialtyId" :value="s.specialtyId">
                                    {{ s.name }}
                                </option>
                            </select>
                        </div>

                        <!-- Behandelcode -->
                        <div>
                            <label for="treatment-code" class="mb-1 block text-sm text-slate-800">
                                {{ t.addCodeLabel }}
                            </label>
                            <select id="treatment-code" v-model="form.treatmentCodeId" @change="onCodeChange"
                                class="w-full rounded-lg border border-slate-400 bg-white px-3 py-2.5 text-sm text-slate-800 focus:border-blue-950 focus:outline-none focus:ring-1 focus:ring-blue-950">
                                <option value="" disabled>{{ t.addCodePlaceholder }}</option>
                                <option v-for="code in availableCodes" :key="code.treatmentCodeId"
                                    :value="code.treatmentCodeId">
                                    {{ code.careCode }} – {{ code.description }}
                                </option>
                            </select>
                        </div>

                        <!-- Duur -->
                        <div>
                            <label for="appointment-duration" class="mb-1 block text-sm text-slate-800">
                                {{ t.durationLabel }}
                            </label>
                            <select id="appointment-duration" v-model="form.durationMinutes"
                                class="w-full rounded-lg border border-slate-400 bg-white px-3 py-2.5 text-sm text-slate-800 focus:border-blue-950 focus:outline-none focus:ring-1 focus:ring-blue-950">
                                <option v-for="minutes in durationOptions" :key="minutes" :value="minutes">
                                    {{ formatText(t.durationUnit, { minutes }) }}
                                </option>
                            </select>
                        </div>

                        <!-- Toelichting -->
                        <div>
                            <label for="specialist-notes" class="mb-1 block text-sm text-slate-800">
                                {{ t.notesLabel }}
                            </label>
                            <textarea id="specialist-notes" v-model="form.notes" rows="4"
                                class="w-full resize-none rounded-lg border border-slate-400 bg-white p-3 text-sm text-slate-900 focus:border-blue-950 focus:outline-none focus:ring-1 focus:ring-blue-950" />
                        </div>

                        <p v-if="formError" role="alert" class="text-sm font-medium text-red-700">
                            {{ formError }}
                        </p>

                        <div class="flex items-center justify-end gap-3">
                            <button v-if="editingId" type="button" @click="resetForm"
                                class="rounded-lg border border-slate-400 bg-white px-4 py-2 text-sm font-bold text-blue-950 transition hover:bg-slate-50">
                                {{ t.cancelEditButton }}
                            </button>
                            <button type="submit"
                                class="inline-flex items-center gap-2 rounded-lg bg-blue-950 px-4 py-2 text-sm font-bold text-white transition hover:bg-blue-900">
                                <Plus v-if="!editingId" class="size-4" aria-hidden="true" />
                                {{ editingId ? t.updateButton : t.addButton }}
                            </button>
                        </div>
                    </form>

                    <!-- Rechterkant: lokale lijst (nog niet opgeslagen) -->
                    <div class="flex min-h-72 flex-col rounded-lg border border-slate-300 bg-slate-50/40 p-3.5">
                        <h2 class="mb-2 text-sm font-semibold text-blue-950">
                            {{ t.pendingHeading }} ({{ pendingReferrals.length }})
                        </h2>

                        <p v-if="!pendingReferrals.length" class="text-sm text-slate-500">
                            {{ t.emptyList }}
                        </p>

                        <div v-else class="overflow-x-auto">
                            <table class="w-full text-left text-sm">
                                <thead>
                                    <tr class="border-b border-slate-300 text-slate-600">
                                        <th class="py-2 pr-2 font-medium">{{ t.colCode }}</th>
                                        <th class="py-2 pr-2 font-medium">{{ t.colDuration }}</th>
                                        <th class="py-2 pr-2 font-medium">{{ t.colNotes }}</th>
                                        <th class="py-2"><span class="sr-only">{{ t.colActions }}</span></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="item in pendingReferrals" :key="item.localId" @click="startEdit(item)"
                                        @keydown.enter="startEdit(item)" tabindex="0"
                                        :class="[
                                            'cursor-pointer border-b border-slate-200 transition hover:bg-blue-50 focus:bg-blue-50 focus:outline-none',
                                            item.localId === editingId ? 'bg-blue-100' : 'bg-white'
                                        ]">
                                        <td class="py-2 pr-2">
                                            <span class="font-semibold text-blue-950">{{ item.careCode }}</span>
                                            <span class="block text-xs text-slate-600">{{ item.description }}</span>
                                        </td>
                                        <td class="py-2 pr-2 whitespace-nowrap">
                                            {{ formatText(t.durationUnit, { minutes: item.durationMinutes }) }}
                                        </td>
                                        <td class="max-w-40 truncate py-2 pr-2 text-slate-600" :title="item.notes">
                                            {{ item.notes || '–' }}
                                        </td>
                                        <td class="py-2 text-right">
                                            <button type="button" @click.stop="removeEntry(item.localId)"
                                                :aria-label="formatText(t.removeCodeLabel, { code: item.careCode })"
                                                class="rounded p-1 text-slate-500 transition hover:bg-red-50 hover:text-red-700">
                                                <Trash2 class="size-4" aria-hidden="true" />
                                            </button>
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>

                <p v-if="errorMessage" role="alert" class="mt-4 text-sm font-medium text-red-700">
                    {{ errorMessage }}
                </p>

                <div class="mt-5 flex items-center justify-between gap-4">
                    <button type="button" @click="handleCancel"
                        class="rounded-lg border border-slate-400 bg-white px-6 py-2.5 text-sm font-bold text-blue-950 transition hover:bg-slate-50">
                        {{ t.cancelButton }}
                    </button>

                    <button type="button" @click="handleSubmit" :disabled="isSubmitting"
                        class="rounded-lg bg-blue-950 px-6 py-2.5 text-sm font-bold text-white transition hover:bg-blue-900 disabled:opacity-50">
                        {{ isSubmitting ? t.submitButtonLoading : t.submitButton }}
                    </button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Plus, Trash2 } from 'lucide-vue-next'
import api from '@/services/api'
import nlPatients from '@/features/patients/nl.json'
import { useToast } from '@/features/shared/useToast.js'

const { showToast } = useToast()

const t = nlPatients.referral

const formatText = (template, vars = {}) =>
    Object.entries(vars).reduce(
        (str, [key, value]) => str.replaceAll(`{${key}}`, String(value)),
        template
    )

const route = useRoute()
const router = useRouter()
const patientId = computed(() => route.params.patientId)

const durationOptions = [15, 30, 45, 60, 75, 90, 105, 120, 135, 150, 165, 180]

// Data van de API
const treatmentCodeOptions = ref([])
const specialties = ref([])
const selectedSpecialtyId = ref('')

// Formulier
const emptyForm = () => ({ treatmentCodeId: '', durationMinutes: 45, notes: '' })
const form = reactive(emptyForm())
const editingId = ref(null) // localId van de regel die je bewerkt
const formError = ref('')

// Lokale lijst (nog niet in de database)
const pendingReferrals = ref([])
let nextLocalId = 1

const errorMessage = ref('')
const isSubmitting = ref(false)

onMounted(async () => {
    try {
        const [codesRes, specialtiesRes] = await Promise.all([
            api.get('/treatment-codes/active'),
            api.get('/specialties')
        ])
        treatmentCodeOptions.value = codesRes.data
        specialties.value = specialtiesRes.data
    } catch (error) {
        errorMessage.value = error.response?.data?.message || t.errors.generic
    }
})

// Codes die nog gekozen mogen worden (de code van de regel die je bewerkt blijft beschikbaar)
const availableCodes = computed(() => {
    const editing = pendingReferrals.value.find(r => r.localId === editingId.value)
    const usedIds = new Set(
        pendingReferrals.value
            .filter(r => r.localId !== editingId.value)
            .map(r => r.treatmentCodeId)
    )
    return treatmentCodeOptions.value.filter(c =>
        !usedIds.has(c.treatmentCodeId) &&
        (
            selectedSpecialtyId.value === '' ||
            c.specialtyId === selectedSpecialtyId.value ||
            c.treatmentCodeId === editing?.treatmentCodeId
        )
    )
})

// Standaardduur overnemen van de gekozen code (alleen bij nieuwe regel)
const onCodeChange = () => {
    if (editingId.value) return
    const match = treatmentCodeOptions.value.find(c => c.treatmentCodeId === form.treatmentCodeId)
    if (match?.durationQuarters) {
        const minutes = match.durationQuarters * 15
        if (durationOptions.includes(minutes)) form.durationMinutes = minutes
    }
}

const resetForm = () => {
    Object.assign(form, emptyForm())
    editingId.value = null
    formError.value = ''
}

const saveEntry = () => {
    formError.value = ''

    if (!form.treatmentCodeId) {
        formError.value = t.errors.noTreatmentCode
        return
    }

    const code = treatmentCodeOptions.value.find(c => c.treatmentCodeId === form.treatmentCodeId)
    if (!code) return

    const entry = {
        treatmentCodeId: code.treatmentCodeId,
        careCode: code.careCode,
        description: code.description,
        specialtyId: code.specialtyId,
        durationMinutes: form.durationMinutes,
        notes: form.notes.trim()
    }

    if (editingId.value) {
        const index = pendingReferrals.value.findIndex(r => r.localId === editingId.value)
        if (index !== -1) {
            pendingReferrals.value[index] = { ...entry, localId: editingId.value }
        }
    } else {
        pendingReferrals.value.push({ ...entry, localId: nextLocalId++ })
    }

    resetForm()
}

const startEdit = (item) => {
    editingId.value = item.localId
    form.treatmentCodeId = item.treatmentCodeId
    form.durationMinutes = item.durationMinutes
    form.notes = item.notes
    formError.value = ''
}

const removeEntry = (localId) => {
    pendingReferrals.value = pendingReferrals.value.filter(r => r.localId !== localId)
    if (editingId.value === localId) resetForm()
}

const handleCancel = () => router.back()

const handleSubmit = async () => {
    errorMessage.value = ''

    if (!pendingReferrals.value.length) {
        errorMessage.value = t.errors.noTreatmentCodes
        return
    }

    isSubmitting.value = true
    try {
        const { data } = await api.post(`/referral/patient/${patientId.value}/referrals`, {
            referrals: pendingReferrals.value.map(r => ({
                treatmentCodeId: r.treatmentCodeId,
                durationMinutes: r.durationMinutes,
                notes: r.notes || null
            }))
        })
        if (data.emailSent) {
            showToast(t.success, 'success')
        } else {
            showToast(t.successEmailFailed, 'warning')
        }
        router.push({ name: 'searchpatient' })
    } catch (error) {
        console.error(error)
        errorMessage.value = error.response?.data?.message || t.errors.generic
    } finally {
        isSubmitting.value = false
    }
}
</script>
