import { describe, expect, it } from 'vitest';

import { AppointmentListItem } from '../../core/models/appointment.model';
import { agendaRange, localDayKey, splitAgenda } from './dashboard-agenda';

const NOW = new Date(2026, 10, 11, 10, 0);

function item(id: number, when: Date, statusId = 1): AppointmentListItem {
  return {
    appointmentId: id,
    patientId: id,
    patientFullName: `Patient ${id}`,
    slotStartUtc: when.toISOString(),
    statusId,
  };
}

describe('splitAgenda', () => {
  it('lists every appointment today in time order, whatever its status', () => {
    const agenda = splitAgenda(
      [
        item(2, new Date(2026, 10, 11, 14, 0), 2),
        item(1, new Date(2026, 10, 11, 9, 0), 1),
        item(3, new Date(2026, 10, 11, 11, 30), 3),
      ],
      NOW,
    );

    expect(agenda.today.map((appointment) => appointment.appointmentId)).toEqual([1, 3, 2]);
  });

  it('lists only scheduled appointments from tomorrow to the end of the window', () => {
    const agenda = splitAgenda(
      [
        item(1, new Date(2026, 10, 12, 9, 0), 1),
        item(2, new Date(2026, 10, 12, 10, 0), 3),
        item(3, new Date(2026, 10, 18, 9, 0), 1),
        item(4, new Date(2026, 10, 19, 9, 0), 1),
      ],
      NOW,
    );

    expect(agenda.upcoming.map((appointment) => appointment.appointmentId)).toEqual([1, 3]);
  });

  it('keeps today out of the upcoming list', () => {
    const agenda = splitAgenda([item(1, new Date(2026, 10, 11, 15, 0), 1)], NOW);

    expect(agenda.today).toHaveLength(1);
    expect(agenda.upcoming).toHaveLength(0);
  });

  it('asks for today through the end of the upcoming window', () => {
    const range = agendaRange(NOW);

    expect(localDayKey(range.from)).toBe('2026-11-11');
    expect(localDayKey(range.to)).toBe('2026-11-19');
  });
});
