import { FileText, FilePlus, Send } from 'lucide-vue-next'

const viewDossier = { key: 'dossier', icon: FileText, route: 'patient-dossier' }
const addToDossier = { key: 'add', icon: FilePlus, route: 'patient-dossier-add' }
const refer = { key: 'refer', icon: Send, route: 'patient-referral' }

// For when the pages get implemented
// GeneralPractitioner: [refer, viewDossier, addToDossier],
export const patientActionsByRole = {
  GeneralPractitioner: [refer],
//   Nurse: [addToDossier],
//   Specialist: [viewDossier, addToDossier],
}