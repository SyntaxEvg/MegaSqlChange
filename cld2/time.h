#ifndef SYS_TIME_H
#define SYS_TIME_H

#include <time.h>
#include <windows.h>

#ifdef __cplusplus
extern "C" {
#endif

    struct timeval {
        long tv_sec;
        long tv_usec;
    };

    static inline int gettimeofday(struct timeval* tp, void* tzp) {
        // ���������� tzp (timezone)
        FILETIME ft;
        ULARGE_INTEGER li;
        UINT64 tt;

        GetSystemTimeAsFileTime(&ft);
        li.LowPart = ft.dwLowDateTime;
        li.HighPart = ft.dwHighDateTime;
        // �������������� �� ������� Windows (100-������������� ��������� � 1 ������ 1601)
        // �� ����� Unix (������� � 1 ������ 1970)
        tt = (li.QuadPart - 116444736000000000ULL) / 10;
        tp->tv_sec = (long)(tt / 1000000);
        tp->tv_usec = (long)(tt % 1000000);
        return 0;
    }

#ifdef __cplusplus
}
#endif

#endif // SYS_TIME_H