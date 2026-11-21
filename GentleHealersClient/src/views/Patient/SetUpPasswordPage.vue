<script setup>
import {useRoute, useRouter} from 'vue-router'
import {ref, onMounted} from 'vue'
import nl from '../../services/nl.json'

const translations = nl.SetUpPasswordPage
const route = useRoute();
const router = useRouter();
const status = ref(translations.checkingStatus)

var token = route.query.token
var email = route.query.email
const isValid = ref(false)
const isChecking = ref(true)
const isDone = ref(false)


onMounted(async() => {
    if (!token || !email) {
        status.value = translations.invalidLinkStatus
        isChecking.value = false
        return
    }
    try {
        const response = await fetch('https://localhost:7113/api/users/validate-activation-token', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                token, email
            })
        })
        if (!response.ok) {
            throw new Error(translations.activationFailed)
        }

        status.value = translations.activatedStatus
        isValid.value = true
    } catch (error) {
        status.value = translations.invalidOrExpiredStatus
    } finally {
        isChecking.value = false
    }
})

const password = ref('')
const confirmpassword = ref('')
const errormessage = ref('')
const succesmessage = ref('')
const showPassword = ref(false)
const showConfirmPassword = ref(false)

async function setPassword() {
    errormessage.value = ''
    succesmessage.value = ''

    if (!password.value || !confirmpassword.value) {
        errormessage.value = translations.requiredFields
        return
    }

    if(password.value !== confirmpassword.value) {
        errormessage.value = translations.passwordMismatch
        return
    }

    if (!isValid.value) {
        errormessage.value = translations.invalidActivationLink
        return
    }

    try {
        const response = await fetch('https://localhost:7113/api/users/set-password', 
        {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                email,
                token,
                password: password.value
            })
        })

        if (!response.ok) {
            throw new Error(translations.setPasswordFailed)
        }

        succesmessage.value = translations.passwordSetSuccess
        status.value = translations.accountActivatedStatus
        isDone.value = true

    } catch {
        errormessage.value = translations.setPasswordFailed
    }

}

</script>
<template>
    <div class="min-h-screen bg-[#e8f3fc] flex items-center justify-center p-6">
    <div class="w-full max-w-[530px] bg-white rounded-2xl shadow-sm border border-slate-300 px-8 py-10">

        <!-- Logo -->
        <div class="text-center text-[1.75rem] font-bold tracking-tight" :aria-label="translations.brandAriaLabel">
            <span class="text-[#12376b]">{{ translations.brandFirst }}</span>
            <span class="text-[#0a76d8]">{{ translations.brandSecond }}</span>
        </div>
        <hr class="mb-6 mt-4 h-1 rounded-sm border-0 bg-[#e3eefb]" />

        <!-- Titel en subtitel -->
        <div v-if="!isDone" class="mb-6">
        <h1 class="text-2xl font-bold text-[#123668]">{{ translations.title }}</h1>
        <p class="text-sm text-[#123668] mt-1">
            {{ translations.subtitle }}
        </p>
        </div>

        <!-- Melding als de token ongeldig is -->
        <div
            v-if="!isChecking && !isValid"
            role="alert"
            class="mb-5 p-5 bg-red-50 border-2 border-red-300 rounded-xl"
        >
            <p class="font-semibold text-red-800">{{ translations.invalidTitle }}</p>
            <p class="text-sm text-red-700 mt-1">
                {{ translations.invalidDescription }}
            </p>
        </div>

        <!-- Succesmelding na het instellen -->
        <div
            v-if="succesmessage"
            role="status"
            class="p-5 bg-green-50 border-2 border-green-300 rounded-xl"
        >
            <p class="font-semibold text-green-800">{{ succesmessage }}</p>
            <RouterLink
                to="/login"
                class="inline-block mt-4 px-5 py-2 rounded-xl bg-[#123668] text-white font-semibold hover:bg-[#0d2a52] transition"
            >
                {{ translations.loginLink }}
            </RouterLink>
        </div>

        <form class="space-y-5" @submit.prevent="setPassword">

            <!-- Nieuw wachtwoord -->
            <div v-if="isValid && !isDone" class="space-y-2">
                <label for="password" class="block text-sm font-semibold text-[#123668]">{{ translations.passwordLabel }}</label>
                <div class="relative">
                <svg class="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-[#123668]" viewBox="0 0 24 24" fill="currentColor">
                    <path fill-rule="evenodd" d="M12 1.5a5.25 5.25 0 0 0-5.25 5.25v3a3 3 0 0 0-3 3v6.75a3 3 0 0 0 3 3h10.5a3 3 0 0 0 3-3v-6.75a3 3 0 0 0-3-3v-3A5.25 5.25 0 0 0 12 1.5Zm3.75 8.25v-3a3.75 3.75 0 1 0-7.5 0v3h7.5Z" clip-rule="evenodd" />
                </svg>

                <input
                    id="password"
                    v-model="password"
                    :type="showPassword ? 'text' : 'password'"
                    :placeholder="translations.passwordPlaceholder"
                    class="w-full pl-12 pr-12 py-3 rounded-xl border-2 border-[#123668] text-[#123668] placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-[#0a78d1]"
                />

                <button type="button" @click="showPassword = !showPassword" :aria-label="showPassword ? translations.hidePassword : translations.showPassword" class="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 hover:text-[#123668] transition">
                    <!-- Doorgestreept oog: wachtwoord is verborgen -->
                    <svg v-if="!showPassword" class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M3 3l18 18" />
                    <path d="M10.6 10.6a2 2 0 0 0 2.8 2.8" />
                    <path d="M9.4 5.5A9.8 9.8 0 0 1 12 5c5 0 8.5 4 9.5 7a11.7 11.7 0 0 1-3 4.4M6.5 6.6A11.7 11.7 0 0 0 2.5 12c1 3 4.5 7 9.5 7 1.5 0 2.9-.4 4.1-1" />
                    </svg>
                    <!-- Open oog: wachtwoord is zichtbaar -->
                    <svg v-else class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M2.5 12C3.5 9 7 5 12 5s8.5 4 9.5 7c-1 3-4.5 7-9.5 7s-8.5-4-9.5-7Z" />
                    <circle cx="12" cy="12" r="3" />
                    </svg>
                </button>
                </div>
            </div>

            <!-- Herhaal wachtwoord -->
            <div v-if="isValid && !isDone" class="space-y-2">
                <label for="confirmPassword" class="block text-sm font-semibold text-[#123668]">{{ translations.confirmPasswordLabel }}</label>
                <div class="relative">
                <svg class="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-[#123668]" viewBox="0 0 24 24" fill="currentColor">
                    <path fill-rule="evenodd" d="M12 1.5a5.25 5.25 0 0 0-5.25 5.25v3a3 3 0 0 0-3 3v6.75a3 3 0 0 0 3 3h10.5a3 3 0 0 0 3-3v-6.75a3 3 0 0 0-3-3v-3A5.25 5.25 0 0 0 12 1.5Zm3.75 8.25v-3a3.75 3.75 0 1 0-7.5 0v3h7.5Z" clip-rule="evenodd" />
                </svg>

                <input
                    id="confirmPassword"
                    v-model="confirmpassword"
                    :type="showConfirmPassword ? 'text' : 'password'"
                    :placeholder="translations.passwordPlaceholder"
                    class="w-full pl-12 pr-12 py-3 rounded-xl border-2 border-[#123668] text-[#123668] placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-[#0a78d1]"
                />

                <button type="button" @click="showConfirmPassword = !showConfirmPassword" :aria-label="showConfirmPassword ? translations.hideConfirmPassword : translations.showConfirmPassword" class="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 hover:text-[#123668] transition">
                    <!-- Doorgestreept oog: wachtwoord is verborgen -->
                    <svg v-if="!showConfirmPassword" class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M3 3l18 18" />
                    <path d="M10.6 10.6a2 2 0 0 0 2.8 2.8" />
                    <path d="M9.4 5.5A9.8 9.8 0 0 1 12 5c5 0 8.5 4 9.5 7a11.7 11.7 0 0 1-3 4.4M6.5 6.6A11.7 11.7 0 0 0 2.5 12c1 3 4.5 7 9.5 7 1.5 0 2.9-.4 4.1-1" />
                    </svg>
                    <!-- Open oog: wachtwoord is zichtbaar -->
                    <svg v-else class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M2.5 12C3.5 9 7 5 12 5s8.5 4 9.5 7c-1 3-4.5 7-9.5 7s-8.5-4-9.5-7Z" />
                    <circle cx="12" cy="12" r="3" />
                    </svg>
                </button>
                </div>
            </div>

            <!-- Knop -->
            <button
                v-if="isValid && !isDone"
                type="submit"
                class="w-full mt-2 py-3 rounded-xl bg-[#123668] text-white text-xl font-semibold hover:bg-[#0d2a52] transition"
            >
                {{ translations.submitButton }}
            </button>

            <!-- Foutmelding -->
            <div
                v-if="errormessage"
                role="alert"
                class="p-4 bg-red-50 border-2 border-red-300 rounded-xl text-sm font-medium text-red-800"
            >
                {{ errormessage }}
            </div>
        </form>
    </div>
    </div>
</template>