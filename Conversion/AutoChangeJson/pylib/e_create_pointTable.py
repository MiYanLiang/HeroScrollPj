#encoding : utf-8

from e_base import *

def export_json(xls, fn):
    f = create_file(fn)
    if f != None:
        reader = xls_reader.XLSReader()
        cfgs = reader.GetSheetByIndex(xls, 13, 2)
        if cfgs != None:
            f.write("{\n")
            s = "\t\"PointTable\": [\n"
            for c in cfgs:
                ri = RowIndex(len(c))
                ss = "\t\t{\n"
                ss += "\t\t\t\"pointId\": \"" + conv_int(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"nextPoint\": \"" + conv_str_bin(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"pointName\": \"" + conv_str_bin(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"eventType\": \"" + conv_int(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"eventId\": \"" + conv_int(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"pointStory\": \"" + conv_str_bin(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"cityIcon\": \"" + conv_int(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"battleBG\": \"" + conv_int(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"battleBGM\": \"" + conv_int(c[ri.Next()]) + "\",\n"
                ss += "\t\t\t\"flag\": \"" + conv_str_bin(c[ri.Next()]) + "\"\n"
                ss += "\t\t},\n"
                s += ss
            s = s[:-2]
            s += "\n"
            s += "\t]\n"
            s += "}"
            f.write(s)
        else:
            print('sheed %s get failed.' % 'cfg')
        f.close()
def export_bin(xls, fn):
    pass